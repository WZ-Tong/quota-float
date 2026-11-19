using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace QuotaFloat
{
    internal sealed partial class MainForm
    {
        // ---------- 数据获取 ----------
        private void StartFetch()
        {
            if (fetching) return;
            fetching = true;

            string dsKey = cfg.DeepSeekKey;
            string goKey = cfg.OpenCodeKey;

            if (string.IsNullOrEmpty(dsKey) && string.IsNullOrEmpty(goKey))
            {
                ApplyFetch(new FetchResult(new DeepSeekState(), new GoState()));
                return;
            }

            Task<FetchResult>.Factory.StartNew(delegate
            {
                DeepSeekState ds = FetchDeepSeek(dsKey);
                GoState go = FetchGo(goKey);
                return new FetchResult(ds, go);
            }).ContinueWith(delegate(Task<FetchResult> t)
            {
                try
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke(new Action(delegate
                    {
                        if (t.IsFaulted || t.IsCanceled)
                            ApplyFetch(new FetchResult(
                                new DeepSeekState { Configured = dsKey.Length > 0, Error = Strings.QueryFailed },
                                new GoState { Configured = goKey.Length > 0, Error = Strings.QueryFailed }));
                        else
                            ApplyFetch(t.Result);
                    }));
                }
                catch { }
            });
        }

        private void ApplyFetch(FetchResult r)
        {
            if (IsDisposed) return;
            fetching = false;
            lastFetch = DateTime.Now;

            DeepSeekState ds = r.DeepSeek;
            if (!ds.Configured)
            {
                if (editingField != 0) EndEdit(false);
                lastDeepSeekBalanceKnown = false;
                deepSeekStatus.Text = Strings.NotConfiguredHint;
                deepSeekStatus.ForeColor = Palette.Sub;
                UpdateDisplay();
            }
            else if (!ds.Ok)
            {
                if (editingField != 0) EndEdit(false);
                lastDeepSeekBalanceKnown = false;
                deepSeekStatus.Text = ds.Error;
                deepSeekStatus.ForeColor = Palette.Red;
                UpdateDisplay();
            }
            else
            {
                lastDeepSeekBalance = ds.Total;
                lastDeepSeekCurrency = ds.Currency;
                lastDeepSeekBalanceKnown = true;

                cfg.DeepSeekBalance = ds.Total;
                cfg.DeepSeekCurrency = ds.Currency;
                cfg.DeepSeekBalanceKnown = true;
                cfg.DeepSeekSpent = cfg.DeepSeekHasTotal
                    ? Math.Max(0m, cfg.DeepSeekTotal - ds.Total)
                    : 0m;
                cfg.Save();

                if (editingField != 0 && cfg.EquationEnabled)
                {
                    string sym = CurrencySymbol(ds.Currency);
                    deepSeekBalance.Text = sym + " " + ds.Total.ToString("0.00", CultureInfo.InvariantCulture);
                }
                else
                {
                    UpdateDisplay();
                }
            }

            GoState go = r.Go;
            ApplyRow(row5h, go, go.Rolling);
            ApplyRow(rowWeek, go, go.Weekly);
            ApplyRow(rowMonth, go, go.Monthly);
            if (edgeHidden || hintAlpha > 0f) Invalidate();
        }

        private static string CurrencySymbol(string currency)
        {
            if (currency == "CNY") return "¥";
            if (currency == "USD") return "$";
            return string.IsNullOrEmpty(currency) ? "" : currency + " ";
        }

        private static void ApplyRow(UsageRow row, GoState go, GoWindow w)
        {
            row.Configured = go.Configured;
            row.Error = go.Ok ? "" : go.Error;
            row.RateLimited = go.Ok && w.RateLimited;
            row.ResetsAt = go.Ok ? w.ResetsAt : null;
            row.Remaining = go.Ok && w.HasData && !w.RateLimited ? Math.Max(0, 100 - w.Used) : -1;
            if (go.Ok && w.RateLimited) row.Remaining = 0;
            row.Invalidate();
        }

        private static DeepSeekState FetchDeepSeek(string key)
        {
            DeepSeekState st = new DeepSeekState();
            if (string.IsNullOrEmpty(key)) return st;
            st.Configured = true;
            int status = 0;
            try
            {
                string json = HttpGet("https://api.deepseek.com/user/balance", key, out status);
                JavaScriptSerializer js = new JavaScriptSerializer();
                Dictionary<string, object> root = js.DeserializeObject(json) as Dictionary<string, object>;
                if (root == null) { st.Error = Strings.ResponseParseFailed; return st; }
                if (root.ContainsKey("is_available") && !Convert.ToBoolean(root["is_available"]))
                {
                    st.Error = Strings.AccountUnavailable;
                    return st;
                }
                object[] infos = root.ContainsKey("balance_infos") ? root["balance_infos"] as object[] : null;
                if (infos == null || infos.Length == 0) { st.Error = Strings.NoBalanceInfo; return st; }
                Dictionary<string, object> pick = null;
                foreach (object it in infos)
                {
                    Dictionary<string, object> d = it as Dictionary<string, object>;
                    if (d == null) continue;
                    if (pick == null) pick = d;
                    if (GetString(d, "currency") == "CNY") { pick = d; break; }
                }
                if (pick == null) { st.Error = Strings.NoBalanceInfo; return st; }
                st.Currency = GetString(pick, "currency");
                st.Total = GetDecimal(pick, "total_balance");
                st.Ok = true;
            }
            catch (Exception ex)
            {
                st.Error = MapError(status, ex);
            }
            return st;
        }

        private static GoState FetchGo(string key)
        {
            GoState st = new GoState();
            if (string.IsNullOrEmpty(key)) return st;
            st.Configured = true;

            string json = null;
            int status = 0;
            Exception last = null;
            string[] urls = new string[]
            {
                "https://opencode.ai/zen/go/v1/usage",
                "https://opencode.ai/inference/go/v1/usage"
            };
            foreach (string url in urls)
            {
                try
                {
                    json = HttpGet(url, key, out status);
                    last = null;
                    break;
                }
                catch (Exception ex)
                {
                    last = ex;
                    if (status != 404) break;
                }
            }
            if (json == null)
            {
                st.Error = MapError(status, last);
                return st;
            }

            try
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                Dictionary<string, object> root = js.DeserializeObject(json) as Dictionary<string, object>;
                if (root == null || !root.ContainsKey("usage")) { st.Error = Strings.MissingUsage; return st; }
                Dictionary<string, object> usage = root["usage"] as Dictionary<string, object>;
                if (usage == null) { st.Error = Strings.MissingUsage; return st; }
                st.Rolling = ParseWindow(usage, "rolling");
                st.Weekly = ParseWindow(usage, "weekly");
                st.Monthly = ParseWindow(usage, "monthly");
                st.Ok = true;
            }
            catch (Exception ex)
            {
                st.Error = Strings.ParseFailed(ex.Message);
            }
            return st;
        }

        private static GoWindow ParseWindow(Dictionary<string, object> usage, string name)
        {
            GoWindow w = new GoWindow();
            object raw;
            if (!usage.TryGetValue(name, out raw)) return w;
            Dictionary<string, object> d = raw as Dictionary<string, object>;
            if (d == null) return w;
            w.HasData = true;
            if (d.ContainsKey("percent"))
                w.Used = (int)Math.Round(Math.Max(0, Math.Min(100, GetDouble(d, "percent"))));
            else if (d.ContainsKey("used") && d.ContainsKey("limit"))
            {
                double limit = GetDouble(d, "limit");
                if (limit > 0)
                    w.Used = (int)Math.Round(Math.Max(0, Math.Min(100, GetDouble(d, "used") * 100.0 / limit)));
            }
            if (d.ContainsKey("status"))
                w.RateLimited = string.Equals(GetString(d, "status"), "rate-limited", StringComparison.OrdinalIgnoreCase);
            if (d.ContainsKey("resetsAt"))
                w.ResetsAt = ParseTime(d["resetsAt"]);
            return w;
        }

        private static DateTimeOffset? ParseTime(object o)
        {
            string s = o as string;
            if (string.IsNullOrEmpty(s)) return null;
            DateTimeOffset dto;
            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dto))
                return dto;
            return null;
        }

        private static string HttpGet(string url, string key, out int status)
        {
            status = 0;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.UserAgent = "QuotaFloat/1.0";
            req.Headers["Authorization"] = "Bearer " + key;
            req.Accept = "application/json";
            req.Timeout = 12000;
            req.ReadWriteTimeout = 12000;
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    status = (int)resp.StatusCode;
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        return sr.ReadToEnd();
                }
            }
            catch (WebException we)
            {
                HttpWebResponse hr = we.Response as HttpWebResponse;
                if (hr != null) status = (int)hr.StatusCode;
                throw;
            }
        }

        private static string MapError(int status, Exception ex)
        {
            if (status == 401) return Strings.InvalidKey(status);
            if (status == 402) return Strings.InsufficientBalance(status);
            if (status == 403) return Strings.Forbidden(status);
            if (status == 429) return Strings.TooManyRequests(status);
            if (status > 0) return "HTTP " + status.ToString(CultureInfo.InvariantCulture);
            WebException we = ex as WebException;
            if (we != null)
            {
                if (we.Status == WebExceptionStatus.Timeout) return Strings.Timeout;
                if (we.Status == WebExceptionStatus.NameResolutionFailure) return Strings.DnsFailure;
                if (we.Status == WebExceptionStatus.ConnectFailure) return Strings.ConnectFailure;
            }
            return Strings.NetworkError;
        }

        private static string GetString(Dictionary<string, object> d, string k)
        {
            object v;
            return d.TryGetValue(k, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";
        }

        private static decimal GetDecimal(Dictionary<string, object> d, string k)
        {
            object v;
            if (!d.TryGetValue(k, out v) || v == null) return 0m;
            try { return Convert.ToDecimal(v, CultureInfo.InvariantCulture); }
            catch { return 0m; }
        }

        private static double GetDouble(Dictionary<string, object> d, string k)
        {
            object v;
            if (!d.TryGetValue(k, out v) || v == null) return 0;
            try { return Convert.ToDouble(v, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }
    }
}
