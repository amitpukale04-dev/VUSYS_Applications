const paths = {
  leaveManagement: (
    <>
      <path d="M4 20V6a2 2 0 0 1 2-2h8l4 4v12H4Z" />
      <path d="M14 4v4h4M8 12h6M8 16h4" />
      <path d="m15 15 2 2 4-4" />
    </>
  ),
  leaveType: (
    <>
      <rect x="4" y="4" width="16" height="17" rx="2" />
      <path d="M8 2v4M16 2v4M4 9h16M8 13h4M8 17h4" />
    </>
  ),
  myLeaveRequests: (
    <>
      <rect x="3" y="4" width="18" height="17" rx="2" />
      <path d="M7 2v4M17 2v4M3 9h18M8 13h4M8 17h3" />
      <path d="m15 16 2 2 3-4" />
    </>
  ),
  ApproveRejectIcon: (
    <>
      <rect x="4" y="3" width="16" height="18" rx="2" />
      <path d="M9 3v3h6V3M8 11h3M8 15h3" />
      <path d="m13 11 2 2 4-4M14 16l4 4M18 16l-4 4" />
    </>
  ),
  CancelLeaveRequestIcon: (
    <>
      <rect x="4" y="3" width="16" height="18" rx="2" />
      <path d="M9 3v3h6V3M8 11h5M8 15h4" />
      <path d="M14 12l5 5M19 12l-5 5" />
    </>
  ),
  ApplyLeaveRequestIcon: (
    <>
      <path d="M13 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h9" />
      <path d="M13 3v5h5" />
      <path d="M8 11h6M8 15h3" />
      <path d="m14 17 5-5 2 2-5 5-3 1 1-3Z" />
      <path d="m16 6 2 2" />
    </>
  ),
  leaveApproval: (
    <>
      <rect x="4" y="3" width="16" height="18" rx="2" />
      <path d="M9 3v3h6V3M8 11h4M8 15h3M14 12l2 2 4-4" />
    </>
  ),
  leaveBalance: (
    <>
      <circle cx="9" cy="8" r="3" />
      <path d="M3 20v-2a6 6 0 0 1 12 0v2M16 8h5M16 12h5M16 16h5" />
    </>
  ),
  holidayList: (
    <>
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M8 3v4M16 3v4M3 10h18" />
      <path d="m9 16 2 2 4-5" />
    </>
  ),
  arrowRight: (
    <>
      <path d="M5 12h14" />
      <path d="m13 6 6 6-6 6" />
    </>
  ),
  search: (
    <>
      <circle cx="11" cy="11" r="7" />
      <path d="m20 20-4-4" />
    </>
  ),
  home: (
    <>
      <path d="M3 10.5 12 3l9 7.5" />
      <path d="M5.5 9.5V21h13V9.5" />
      <path d="M9.5 21v-7h5v7" />
    </>
  ),
  timesheet: (
    <>
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M8 3v4M16 3v4M3 10h18" />
      <path d="M7 14h3M14 14h3M7 18h3M14 18h3" />
    </>
  ),
  timeEntry: (
    <>
      <rect x="3" y="4" width="14" height="17" rx="2" />
      <path d="M7 2v4M13 2v4M3 9h14" />
      <path d="M7 13h4M7 17h3" />
      <circle cx="18" cy="16" r="4" />
      <path d="M18 14v2.4l1.5 1" />
    </>
  ),
  downloadtimesheet: (
    <>
      <rect x="3" y="4" width="18" height="16" rx="2" />
      <path d="M8 2v4M16 2v4M3 9h18" />
      <path d="M7 13h3M7 16h2" />
      <path d="M16 11v7" />
      <path d="m13 15 3 3 3-3" />
      <path d="M13 20h6" />
    </>
  ),
  timesheetSubmit: (
    <>
      <path d="M6 3h9l4 4v14H6z" />
      <path d="M15 3v5h5" />
      <path d="M9 12h5M9 16h4" />
      <path d="M4 9v8" />
      <path d="m2 15 2 2 2-2" />
    </>
  ),
  timesheetApproval: (
    <>
      <rect x="4" y="3" width="16" height="18" rx="2" />
      <path d="M9 3.5h6v3H9z" />
      <path d="M8 11h3M8 15h3" />
      <path d="m14 11 1.5 1.5L18 10" />
      <path d="m14.5 15 3 3M17.5 15l-3 3" />
    </>
  ),
  approval: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="m8 12 2.7 2.7L16.5 9" />
    </>
  ),
  calendar: (
    <>
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M8 3v4M16 3v4M3 10h18" />
      <path d="M8 14h.01M12 14h.01M16 14h.01M8 18h.01M12 18h.01" />
    </>
  ),
  history: (
    <>
      <path d="M3 12a9 9 0 1 0 3-6.7" />
      <path d="M3 4v6h6M12 7v5l3 2" />
    </>
  ),
  project: (
    <>
      <rect x="3" y="4" width="18" height="16" rx="2" />
      <path d="M8 4v16M16 4v16M8 10h8M8 15h8" />
    </>
  ),
  task: (
    <>
      <path d="M9 6h11M9 12h11M9 18h11" />
      <path d="m3 6 1 1 2-2M3 12l1 1 2-2M3 18l1 1 2-2" />
    </>
  ),
  client: (
    <>
      <path d="M4 21V7l8-4 8 4v14" />
      <path d="M8 21v-5h8v5M8 9h.01M12 9h.01M16 9h.01M8 13h.01M12 13h.01M16 13h.01" />
    </>
  ),
  employees: (
    <>
      <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" />
    </>
  ),
  department: (
    <>
      <rect x="3" y="3" width="18" height="18" rx="2" />
      <path d="M9 3v18M9 8h12M9 14h12" />
    </>
  ),
  designation: (
    <>
      <path d="M8 7V3h8v4" />
      <rect x="3" y="7" width="18" height="14" rx="2" />
      <path d="M3 12h18M9 12v2h6v-2" />
    </>
  ),
  report: (
    <>
      <path d="M4 20V10M10 20V4M16 20v-7M22 20H2" />
    </>
  ),
  reportSearch: (
    <>
      <path d="M4 3h11l4 4v6" />
      <path d="M15 3v5h5" />
      <path d="M6 8h5M6 12h4M6 16h3" />
      <circle cx="16" cy="17" r="3" />
      <path d="m18.2 19.2 2.3 2.3" />
    </>
  ),
  exportExcel: (
    <>
      <path d="M5 3h10l4 4v14H5z" />
      <path d="M15 3v5h5" />
      <path d="m8 12 5 6M13 12l-5 6" />
    </>
  ),
  downloadHolidayPdf: (
    <>
      <path d="M5 2.5h9l5 5v14H5z" />
      <path d="M14 2.5v5h5" />
      <path d="M8 12h8M8 15h8" />
      <path d="M12 17v3m-2-2 2 2 2-2" />
    </>
  ),
  downloadReport: (
    <>
      <path d="M5 3h10l4 4v6" />
      <path d="M15 3v5h5" />
      <path d="M12 11v9" />
      <path d="m8.5 16.5 3.5 3.5 3.5-3.5" />
    </>
  ),

  addTimesheet: (
    <>
      <rect x="2.5" y="4.5" width="19" height="17" rx="2" />
      <path d="M7 2.5v4M17 2.5v4M2.5 9.5h19" />
      <path d="M12 12.5v6M9 15.5h6" />
    </>
  ),

  viewTimesheet: (
    <>
      <path d="M8 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h11a2 2 0 0 0 2-2v-3" />
      <path d="M8 3v5h5M8 12h4M8 16h2" />
      <path d="M13 11s2.2-3 5-3 5 3 5 3-2.2 3-5 3-5-3-5-3Z" />
      <circle cx="18" cy="11" r="1.3" />
    </>
  ),

  downloadTimesheet: (
    <>
      <path d="M5 3h9l4 4v5M14 3v5h4M5 3v17a1 1 0 0 0 1 1h7" />
      <path d="M8 12h6M8 16h4" />
      <path d="M18 14v7m-3-3 3 3 3-3" />
    </>
  ),

  downloadDateWiseTimesheet: (
    <>
      <rect x="3" y="4" width="15" height="16" rx="2" />
      <path d="M7 2v4M14 2v4M3 9h15M7 13h3M7 16h3" />
      <path d="M19 13v8m-3-3 3 3 3-3" />
    </>
  ),

  downloadProjectWiseTimesheet: (
    <>
      <path d="M3 6a2 2 0 0 1 2-2h5l2 2h5a2 2 0 0 1 2 2v4H3Z" />
      <path d="M3 12v6a2 2 0 0 0 2 2h8" />
      <path d="M18 13v8m-3-3 3 3 3-3" />
    </>
  ),
  utilization: (
    <>
      <path d="M4 19h16" />
      <path d="M7 16V9M12 16V5M17 16v-3" />
      <circle cx="17" cy="6" r="2" />
    </>
  ),
  missing: (
    <>
      <path d="M12 9v4M12 17h.01" />
      <path d="M10.3 3.5 2.8 17a2 2 0 0 0 1.7 3h15a2 2 0 0 0 1.7-3L13.7 3.5a2 2 0 0 0-3.4 0Z" />
    </>
  ),
  settings: (
    <>
      <circle cx="12" cy="12" r="3" />
      <path d="M19.4 15a1.7 1.7 0 0 0 .34 1.88l.06.06-2.83 2.83-.06-.06A1.7 1.7 0 0 0 15 19.4a1.7 1.7 0 0 0-1 .6 1.7 1.7 0 0 0-.4 1.1V21H9.6v-.1A1.7 1.7 0 0 0 8 19.4a1.7 1.7 0 0 0-1.88.34l-.06.06-2.83-2.83.06-.06A1.7 1.7 0 0 0 3.6 15a1.7 1.7 0 0 0-.6-1 1.7 1.7 0 0 0-1.1-.4H2V9.6h.1A1.7 1.7 0 0 0 3.6 8a1.7 1.7 0 0 0-.34-1.88l-.06-.06 2.83-2.83.06.06A1.7 1.7 0 0 0 8 3.6a1.7 1.7 0 0 0 1-.6 1.7 1.7 0 0 0 .4-1.1V2h4v.1A1.7 1.7 0 0 0 15 3.6a1.7 1.7 0 0 0 1.88-.34l.06-.06 2.83 2.83-.06.06A1.7 1.7 0 0 0 19.4 8c.15.38.4.72.73.98.3.23.68.37 1.07.42h.1v4h-.1a1.7 1.7 0 0 0-1.8 1.6Z" />
    </>
  ),
  security: (
    <>
      <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z" />
      <path d="m9 12 2 2 4-4" />
    </>
  ),
  securityAccess: (
    <>
      <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z" />
      <path d="M8.5 12h7" />
      <path d="M12 8.5v7" />
    </>
  ),
  holiday: (
    <>
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M8 3v4M16 3v4M3 10h18" />
      <path d="m8 15 2 2 5-5" />
    </>
  ),
  role: (
    <>
      <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z" />
      <path d="m9 12 2 2 4-4" />
    </>
  ),
  menuAccess: (
    <>
      <rect x="3" y="3" width="18" height="18" rx="2" />
      <path d="M8 8h8M8 12h5M8 16h3" />
      <path d="m15 15 2 2 4-4" />
    </>
  ),
  logout: (
    <>
      <path d="M10 17l5-5-5-5M15 12H3" />
      <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4" />
    </>
  ),
  yes: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="m8 12 2.5 2.5L16 9" />
    </>
  ),
  no: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="m9 9 6 6M15 9l-6 6" />
    </>
  ),
  chevron: <path d="m9 18 6-6-6-6" />,
  up: (
    <>
      <path d="m18 15-6-6-6 6" />
      <path d="M12 9v12" />
    </>
  ),
  clock: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </>
  ),
  eye: (
    <>
      <path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6S2 12 2 12Z" />
      <circle cx="12" cy="12" r="2.5" />
    </>
  ),
  more: (
    <>
      <circle cx="12" cy="5" r="1" fill="currentColor" stroke="none" />
      <circle cx="12" cy="12" r="1" fill="currentColor" stroke="none" />
      <circle cx="12" cy="19" r="1" fill="currentColor" stroke="none" />
    </>
  ),
  edit: (
    <>
      <path d="M12 20h9" />
      <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z" />
    </>
  ),
  save: (
    <>
      <path d="M5 3h12l2 2v16H5z" />
      <path d="M8 3v6h8V3M8 21v-7h8v7" />
    </>
  ),
  back: (
    <>
      <path d="m15 18-6-6 6-6" />
      <path d="M9 12h11" />
    </>
  ),
  delete: (
    <>
      <path d="M3 6h18M8 6V4h8v2M19 6l-1 15H6L5 6M10 11v6M14 11v6" />
    </>
  ),
  filter: <path d="M4 5h16l-6.5 7.2V19l-3 1v-7.8Z" />,
  close: <path d="m6 6 12 12M18 6 6 18" />,
  sort: (
    <>
      <path d="m8 7 4-4 4 4M12 3v18" />
      <path d="m8 17 4 4 4-4" />
    </>
  ),
  sortUp: (
    <>
      <path d="m8 7 4-4 4 4M12 3v18" />
    </>
  ),
  sortDown: (
    <>
      <path d="M12 3v18M8 17l4 4 4-4" />
    </>
  ),
  first: (
    <>
      <path d="M7 5v14M18 6l-6 6 6 6" />
    </>
  ),
  previous: <path d="m15 18-6-6 6-6" />,
  next: <path d="m9 18 6-6-6-6" />,
  last: (
    <>
      <path d="M17 5v14M6 6l6 6-6 6" />
    </>
  ),
  add: (
    <>
      <path d="M12 5v14M5 12h14" />
    </>
  ),
  refresh: (
    <>
      <path d="M20 11a8 8 0 1 0-2.34 5.66" />
      <path d="M20 4v7h-7" />
    </>
  ),
  clear: (
    <>
      <path d="m4 15 8.5-8.5a2.1 2.1 0 0 1 3 0l2 2a2.1 2.1 0 0 1 0 3L9 20H5l-2-2Z" />
      <path d="m10 9 5 5" />
      <path d="M13 20h8" />
    </>
  ),
  warning: (
    <>
      <path d="M12 9v4M12 17h.01" />
      <path d="M10.3 3.5 2.8 17a2 2 0 0 0 1.7 3h15a2 2 0 0 0 1.7-3L13.7 3.5a2 2 0 0 0-3.4 0Z" />
    </>
  ),
};

export default function SvgIcon({
  name,
  size = 20,
  strokeColor = "",
  className = "",
  strokeWidth = 1.8,
}) {
  return (
    <svg
      className={className}
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke={strokeColor || "currentColor"}
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      {paths[name] || paths.home}
    </svg>
  );
}
