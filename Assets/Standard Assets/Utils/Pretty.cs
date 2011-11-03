using System;
using UnityEngine;

public class Pretty
{
    public static string Seconds(float s) {
        if (s < 0.1f) {
            return Mathf.Round(s*1000).ToString("0 ms");
        } else if (s < 10.0f) {
            return s.ToString("0.0 sec");
        } else if (s < 60.0f) {
            return Mathf.Round(s).ToString("0 sec");
        } else if (s < 60f*60f) {
            int sec = Mathf.RoundToInt(s);
            int min = sec / 60;
            sec -= min*60;
            return String.Format( "{0:0}:{1:00}", min, sec );
        } else {
            int sec = Mathf.RoundToInt(s);
            int min = sec / 60;
            sec -= min*60;
            int hrs = min / 60;
            min -= hrs*60;
            return String.Format( "{0:0}:{1:00}:{2:00}", hrs, min, sec );
        }
    }

    public static string Count(float cnt) {
        if (cnt < 1000f) {
            return cnt.ToString("0");
        } else if (cnt < 1e4f) {
            return (cnt*1e-3f).ToString("0.0k");
        } else if (cnt < 1e6f) {
            return (cnt*1e-3f).ToString("0k");
        } else if (cnt < 1e7f) {
            return (cnt*1e-6f).ToString("0.0M");
        } else if (cnt < 1e9f) {
            return (cnt*1e-6f).ToString("0M");
        } else if (cnt < 1e10f) {
            return (cnt*1e-9f).ToString("0.0G");
        } else {
            return (cnt*1e-6f).ToString("0G");
        }
    }

    public class Exception : System.ApplicationException
    {
        public Exception(string format, params object[] args) : base(string.Format(format,args)) { }
    }

    public class AssertionFailed : Exception
    {
        public AssertionFailed(string format, params object[] args) : base(format,args) { }
    }

}


