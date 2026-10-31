using System.Reflection;

#pragma warning disable F64_NUM, CS1591
public static class F64 {
    public static fix C0 => fix.FromRaw(0L);
    public static fix C1 => fix.FromRaw(4294967296L);
    public static fix C180 => fix.FromRaw(773094113280L);
    public static fix C2 => fix.FromRaw(8589934592L);
    public static fix C3 => fix.FromRaw(12884901888L);
    public static fix C5 => fix.FromRaw(21474836480L);
    public static fix C6 => fix.FromRaw(25769803776L);
    public static fix C16 => fix.FromRaw(68719476736L);
    public static fix C24 => fix.FromRaw(103079215104L);
    public static fix C50 => fix.FromRaw(214748364800L);
    public static fix C60 => fix.FromRaw(257698037760L);
    public static fix C120 => fix.FromRaw(515396075520L);
    public static fix C0p5 => fix.FromRaw(2147483648L);
    public static fix C0p25 => fix.FromRaw(1073741824L);
    public static fix Cm0p9999 => fix.FromRaw(-4294537799L);
    public static fix C1m1em12 => fix.FromRaw(4294967296L);
    public static fix CPiOver180 => fix.FromRaw(74961320L);
    public static fix C180OverPi => fix.FromRaw(246083499207L);

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Fix/PrintRawValues")]
    private static void PrintRawValues() {
        var sb = new System.Text.StringBuilder();
        var rawValueField = typeof(fix).GetField(nameof(fix.RawValue), BindingFlags.Instance | BindingFlags.Public);
        foreach (var methodInfo in typeof(F64).GetMethods(BindingFlags.Public | BindingFlags.Static)) {
            if (methodInfo.ReturnType != typeof(fix)) continue;
            var fixValue = (fix)methodInfo.Invoke(null, null);
            var rawValue = (long)rawValueField.GetValue(fixValue);
            sb.AppendLine("public static fix " + methodInfo.Name.Substring("get_".Length) + " => fix.FromRaw(" +
                          rawValue + "L);");
        }

        UnityEngine.Debug.Log(sb.ToString());
    }
#endif
}