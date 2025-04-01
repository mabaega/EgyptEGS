using System.Collections.Generic;

namespace EgyptEGS.ApiClient.Model
{
    public static class TaxTypeReference
    {
        private static readonly HashSet<(string TaxType, string SubType)> _taxTypes = new()
        {
            ("T1", "V001"), ("T1", "V002"), ("T1", "V003"), ("T1", "V004"), ("T1", "V005"),
            ("T1", "V006"), ("T1", "V007"), ("T1", "V008"), ("T1", "V009"), ("T1", "V010"),
            ("T2", "Tbl01"), ("T3", "Tbl02"),
            ("T4", "W001"), ("T4", "W002"), ("T4", "W003"), ("T4", "W004"), ("T4", "W005"),
            ("T4", "W006"), ("T4", "W007"), ("T4", "W008"), ("T4", "W009"), ("T4", "W010"),
            ("T4", "W011"), ("T4", "W012"), ("T4", "W013"), ("T4", "W014"), ("T4", "W015"),
            ("T4", "W016"),
            ("T5", "ST01"), ("T6", "ST02"),
            ("T7", "Ent01"), ("T7", "Ent02"),
            ("T8", "RD01"), ("T8", "RD02"),
            ("T9", "SC01"), ("T9", "SC02"),
            ("T10", "Mn01"), ("T10", "Mn02"),
            ("T11", "MI01"), ("T11", "MI02"),
            ("T12", "OF01"), ("T12", "OF02"),
            ("T13", "ST03"), ("T14", "ST04"),
            ("T15", "Ent03"), ("T15", "Ent04"),
            ("T16", "RD03"), ("T16", "RD04"),
            ("T17", "SC03"), ("T17", "SC04"),
            ("T18", "Mn03"), ("T18", "Mn04"),
            ("T19", "MI03"), ("T19", "MI04"),
            ("T20", "OF03"), ("T20", "OF04")
        };
        public static HashSet<string> GetValidTaxTypes() => 
            _taxTypes.Select(t => t.TaxType).ToHashSet();

        public static bool IsValidSubTypeForTaxType(string taxType, string subType) => 
            _taxTypes.Contains((taxType?.Trim() ?? string.Empty, subType?.Trim() ?? string.Empty));
    }
}