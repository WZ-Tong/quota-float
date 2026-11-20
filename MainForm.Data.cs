using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private int CurrentRefreshSeconds()
        {
            return edgeHidden ? Timing.HiddenRefreshSeconds : Timing.VisibleRefreshSeconds;
        }

        private double SecondsSince(long stamp)
        {
            if (stamp == Timing.NoStamp) return double.MaxValue;
            return ElapsedSeconds(stamp);
        }

        private double ElapsedSeconds(long stamp)
        {
            if (stamp == Timing.NoStamp) return 0.0;
            return (clock.ElapsedTicks - stamp) / (double)Stopwatch.Frequency;
        }

        private double ElapsedMilliseconds(long stamp)
        {
            return ElapsedSeconds(stamp) * Timing.MillisecondsPerSecond;
        }

        private double OldestFetchAgeSeconds()
        {
            double age = -1.0;
            if (lastDeepSeekFetchStamp != Timing.NoStamp)
                age = SecondsSince(lastDeepSeekFetchStamp);
            if (lastOpenCodeFetchStamp != Timing.NoStamp)
            {
                double goAge = SecondsSince(lastOpenCodeFetchStamp);
                if (age < 0.0 || goAge > age) age = goAge;
            }
            return age;
        }

        private float NextRefreshFraction()
        {
            int interval = CurrentRefreshSeconds();
            float fraction = 1f;
            bool any = false;
            if (cfg.DeepSeekKey.Length > 0)
            {
                fraction = Math.Min(fraction, RemainingFraction(lastDeepSeekFetchStamp, interval));
                any = true;
            }
            if (cfg.OpenCodeKey.Length > 0)
            {
                fraction = Math.Min(fraction, RemainingFraction(lastOpenCodeFetchStamp, interval));
                any = true;
            }
            return any ? fraction : 1f;
        }

        private float RemainingFraction(long stamp, int interval)
        {
            if (stamp == Timing.NoStamp) return 0f;
            double remaining = 1.0 - SecondsSince(stamp) / interval;
            return (float)Math.Max(0.0, Math.Min(1.0, remaining));
        }

        private void StartDueFetches()
        {
            int interval = CurrentRefreshSeconds();
            if (cfg.DeepSeekKey.Length > 0 && !fetchingDeepSeek &&
                SecondsSince(lastDeepSeekFetchStamp) >= interval)
                StartDeepSeekFetch();
            if (cfg.OpenCodeKey.Length > 0 && !fetchingOpenCode &&
                SecondsSince(lastOpenCodeFetchStamp) >= interval)
                StartOpenCodeFetch();
        }

        private void RefreshIfStaleOnReveal()
        {
            if (demoMode || refreshingPaused) return;
            if (cfg.DeepSeekKey.Length > 0 && !fetchingDeepSeek &&
                SecondsSince(lastDeepSeekFetchStamp) >= Timing.RevealRefreshMinAgeSeconds)
                StartDeepSeekFetch();
            if (cfg.OpenCodeKey.Length > 0 && !fetchingOpenCode &&
                SecondsSince(lastOpenCodeFetchStamp) >= Timing.RevealRefreshMinAgeSeconds)
                StartOpenCodeFetch();
        }

        private void StartFetch()
        {
            StartDeepSeekFetch();
            StartOpenCodeFetch();
        }

        private void StartDeepSeekFetch()
        {
            if (fetchingDeepSeek) return;
            fetchingDeepSeek = true;
            dsRefreshButton.SetBusy(true, clock.ElapsedMilliseconds);
            UpdateFetchUi();

            string key = cfg.DeepSeekKey;
            if (string.IsNullOrEmpty(key))
            {
                ApplyDeepSeek(new DeepSeekState());
                return;
            }

            Task<DeepSeekState>.Factory.StartNew(delegate
            {
                return FetchDeepSeek(key);
            }).ContinueWith(delegate(Task<DeepSeekState> t)
            {
                try
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke(new Action(delegate
                    {
                        DeepSeekState state;
                        if (t.IsFaulted || t.IsCanceled)
                            state = new DeepSeekState
                            {
                                Configured = key.Length > 0,
                                Error = Strings.QueryFailed
                            };
                        else
                            state = t.Result;
                        ApplyDeepSeek(state);
                    }));
                }
                catch { }
            });
        }

        private void StartOpenCodeFetch()
        {
            if (fetchingOpenCode) return;
            fetchingOpenCode = true;
            goRefreshButton.SetBusy(true, clock.ElapsedMilliseconds);
            UpdateFetchUi();

            string key = cfg.OpenCodeKey;
            if (string.IsNullOrEmpty(key))
            {
                ApplyOpenCode(new GoState());
                return;
            }

            Task<GoState>.Factory.StartNew(delegate
            {
                return FetchGo(key);
            }).ContinueWith(delegate(Task<GoState> t)
            {
                try
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke(new Action(delegate
                    {
                        GoState state;
                        if (t.IsFaulted || t.IsCanceled)
                            state = new GoState
                            {
                                Configured = key.Length > 0,
                                Error = Strings.QueryFailed
                            };
                        else
                            state = t.Result;
                        ApplyOpenCode(state);
                    }));
                }
                catch { }
            });
        }

        private void UpdateFetchUi()
        {
            bool any = AnyFetching;
            refreshBar.Indeterminate = any;
            if (any)
            {
                refreshBar.Phase = (float)((clock.ElapsedMilliseconds %
                    Timing.ShimmerPeriodMilliseconds) / (double)Timing.ShimmerPeriodMilliseconds);
            }
            refreshBar.Invalidate();
        }

        private void ApplyDeepSeek(DeepSeekState ds)
        {
            if (IsDisposed) return;
            fetchingDeepSeek = false;
            lastDeepSeekFetchStamp = clock.ElapsedTicks;
            dsRefreshButton.SetBusy(false, clock.ElapsedMilliseconds);
            UpdateFetchUi();

            if (!ds.Configured)
            {
                if (editingField != UiMetrics.EquationFieldNone) EndEdit(false);
                lastDeepSeekBalanceKnown = false;
                deepSeekStatus.Text = Strings.NotConfiguredHint;
                deepSeekStatus.ForeColor = Palette.Sub;
                UpdateDisplay();
            }
            else if (!ds.Ok)
            {
                if (editingField != UiMetrics.EquationFieldNone) EndEdit(false);
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

                if (editingField != UiMetrics.EquationFieldNone && cfg.EquationEnabled)
                {
                    string sym = CurrencySymbol(ds.Currency);
                    deepSeekBalance.Text = sym + " " + ds.Total.ToString("0.00", CultureInfo.InvariantCulture);
                }
                else
                {
                    UpdateDisplay();
                }
            }

            if (edgeHidden || hintAlpha > 0f) Invalidate();
        }

        private void ApplyOpenCode(GoState go)
        {
            if (IsDisposed) return;
            fetchingOpenCode = false;
            lastOpenCodeFetchStamp = clock.ElapsedTicks;
            goRefreshButton.SetBusy(false, clock.ElapsedMilliseconds);
            UpdateFetchUi();

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

        private void ApplyRow(UsageRow row, GoState go, GoWindow w)
        {
            row.Configured = go.Configured;
            row.Error = go.Ok ? "" : go.Error;
            row.RateLimited = go.Ok && w.RateLimited;
            row.ResetsAt = go.Ok ? w.ResetsAt : null;
            int remaining = go.Ok && w.HasData && !w.RateLimited
                ? Math.Max(0, UiMetrics.PercentMax - w.Used) : -1;
            if (go.Ok && w.RateLimited) remaining = 0;
            row.SetAnimatedRemaining(remaining, clock.ElapsedMilliseconds, !edgeHidden);
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
                    if (status != (int)HttpStatusCode.NotFound) break;
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
                w.Used = (int)Math.Round(Math.Max(0, Math.Min(UiMetrics.PercentMax,
                    GetDouble(d, "percent"))));
            else if (d.ContainsKey("used") && d.ContainsKey("limit"))
            {
                double limit = GetDouble(d, "limit");
                if (limit > 0)
                    w.Used = (int)Math.Round(Math.Max(0, Math.Min(UiMetrics.PercentMax,
                        GetDouble(d, "used") * UiMetrics.PercentMax / limit)));
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
            req.Timeout = Timing.NetworkTimeoutMilliseconds;
            req.ReadWriteTimeout = Timing.NetworkTimeoutMilliseconds;
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
            if (status == (int)HttpStatusCode.Unauthorized) return Strings.InvalidKey(status);
            if (status == (int)HttpStatusCode.PaymentRequired) return Strings.InsufficientBalance(status);
            if (status == (int)HttpStatusCode.Forbidden) return Strings.Forbidden(status);
            if (status == NetworkMetrics.TooManyRequestsStatus) return Strings.TooManyRequests(status);
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
