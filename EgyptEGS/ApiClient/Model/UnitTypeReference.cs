using System.Collections.Generic;

namespace EgyptEGS.ApiClient.Model
{
    public static class UnitTypeReference
    {
        private static readonly HashSet<string> _unitTypes = new()
        {
            "2Z", "4K", "4O", "A87", "A93", "A94", "AMP", "ANN", "B22", "B49", "B75", "B78", "B84",
            "BAR", "BBL", "BG", "BO", "BOX", "C10", "C39", "C41", "C45", "C62", "CA", "CMK", "CMQ",
            "CMT", "CS", "CT", "CTL", "D10", "D33", "D41", "DAY", "DMT", "DRM", "EA", "FAR", "FOT",
            "FTK", "FTQ", "G42", "GL", "GLL", "GM", "GPT", "GRM", "H63", "HHP", "HLT", "HTZ", "HUR",
            "IE", "INH", "INK", "JOB", "KGM", "KHZ", "KMH", "KMK", "KMQ", "KMT", "KSM", "KVT", "KWT",
            "LB", "LTR", "LVL", "M", "MAN", "MAW", "MGM", "MHZ", "MIN", "MMK", "MMQ", "MMT", "MON",
            "MTK", "MTQ", "OHM", "ONZ", "PAL", "PF", "PK", "SK", "SMI", "ST", "TNE", "TON", "VLT",
            "WEE", "WTT", "X03", "YDQ", "YRD", "NMP", "5I", "AE", "B4", "BB", "BD", "BE", "BK", "BL",
            "CH", "CR", "DAA", "DTN", "DZN", "FP", "HMT", "INQ", "KG", "KTM", "LO", "MLT", "MT", "NA",
            "NAR", "NC", "NE", "NPL", "NV", "PA", "PG", "PL", "PR", "PT", "RL", "RO", "SET", "STK",
            "T3", "TC", "TK", "TN", "TTS", "UC", "VI", "VQ", "YDK", "Z3"
        };
        public static bool ValidateUnitType(string code)
        {
            string normalizedCode = code?.ToUpperInvariant() ?? string.Empty;
            bool isValid = _unitTypes.Contains(normalizedCode);
            return isValid;
        }
    }
}