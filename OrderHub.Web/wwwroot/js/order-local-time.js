export function formatLocalTime(utcIso, locale = navigator.language) {
    const date = new Date(utcIso);
    if (!Number.isFinite(date.getTime())) return "التاريخ غير متاح";
    return new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "medium" }).format(date);
}

// Convert a selected browser-local calendar day to UTC bounds. DST days can
// contain 23 or 25 hours; do not add 24 hours to the first UTC instant.
export function localDayBounds(year, month, day) {
    return {
        fromUtc: new Date(year, month - 1, day).toISOString(),
        toUtcExclusive: new Date(year, month - 1, day + 1).toISOString()
    };
}
