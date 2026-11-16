
export function formatBeirutDate(
  value: string | null | undefined
): string {
  if (!value) return "—";

  // Backend timestamps are UTC.
  // If ASP.NET Core omitted the timezone suffix,
  // interpret the value as UTC, not browser-local time.
  const hasTimezone = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value);
  const utcValue = hasTimezone ? value : `${value}Z`;

  const date = new Date(utcValue);

  if (Number.isNaN(date.getTime())) {
    return "Invalid date";
  }

  return new Intl.DateTimeFormat("en-GB", {
    timeZone: "Asia/Beirut",
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(date);
}
