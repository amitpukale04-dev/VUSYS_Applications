import { useMemo } from "react";
import { useNavigate } from "react-router-dom";
import SvgIcon from "../../components/common/SvgIcon";
import { useAuth } from "../../context/AuthContext";
import useUserSecurityPermission from "../../hooks/useUserSecurityPermission";
import { hasPathAccess } from "../../utils/menuUtils";
import { normalizeText } from "../../utils/valueUtils";
import { ROUTE_CONSTANTS } from "../../constants/RouteConstants";

const HOME_ACTION_CARDS = [
  {
    key: "timesheet-approval-rejection-list",
    permission: { screen: "Approval/Rejection List", action: "Open" },
    roles: ["admin"],
    title: "Timesheet Approval/Rejection List",
    description: "Review employee timesheets and take approval actions.",
    path: ROUTE_CONSTANTS.MANAGE_TIMESHEET.TIMESHEET_APPROVAL_REJECTION_LIST,
    icon: "timesheetApproval",
    tone: "violet",
    actionLabel: "Open Timesheet Approval List",
    eyebrow: "Manager Workspace",
  },
  {
    key: "leave-approval-rejection-list",
    permission: { screen: "Leave Approval/Rejection List", action: "Open" },
    roles: ["admin"],
    title: "Leave Approval/Rejection List",
    description: "Review employee leave requests and approve or reject them.",
    path: ROUTE_CONSTANTS.LEAVE_MANAGEMENT.LEAVE_APPROVAL_REJECTION_LIST,
    icon: "ApproveRejectIcon",
    tone: "emerald",
    actionLabel: "Open Leave Approval List",
    eyebrow: "Leave Management",
  },
  {
    key: "log-timesheet",
    permission: { screen: "Log Timesheet", action: "Open" },
    roles: ["employee"],
    title: "Log Timesheet",
    description: "Record your project activities and daily working hours.",
    path: ROUTE_CONSTANTS.MANAGE_TIMESHEET.LOG_TIMESHEET,
    icon: "timeEntry",
    tone: "coral",
    actionLabel: "Log Timesheet",
    eyebrow: "Daily Activity",
  },
  {
    key: "timesheet-submission",
    permission: { screen: "Timesheet Submission", action: "Open" },
    roles: ["employee"],
    title: "Submit Timesheet for Approval",
    description:
      "Review your monthly hours and send the timesheet for approval.",
    path: ROUTE_CONSTANTS.MANAGE_TIMESHEET.TIMESHEET_SUBMISSION,
    icon: "timesheetSubmit",
    tone: "cyan",
    actionLabel: "Open Submission",
    eyebrow: "Monthly Submission",
  },
  {
    key: "my-leave-requests/new",
    permission: { screen: "My Leave Requests", action: "Apply" },
    roles: ["employee"],
    title: "Apply Leave",
    description: "Submit a new leave request for approval.",
    path: ROUTE_CONSTANTS.LEAVE_MANAGEMENT.APPLY_LEAVE,
    icon: "leaveType",
    tone: "teal",
    actionLabel: "Apply for Leave",
    eyebrow: "New Leave Request",
  },
  {
    key: "my-leave-requests",
    permission: { screen: "My Leave Requests", action: "Open" },
    roles: ["employee"],
    title: "My Leave Request List",
    description: "View and track the status of your leave requests.",
    path: ROUTE_CONSTANTS.LEAVE_MANAGEMENT.MY_LEAVE_REQUESTS,
    icon: "myLeaveRequests",
    tone: "amber",
    actionLabel: "View My Leave Requests",
    eyebrow: "Leave Requests",
  },
];

export default function HomePage() {
  const navigate = useNavigate();
  const { user, menuItems = [], menuAccessLoading } = useAuth();
  const { hasPermission, isLoading: securityPermissionLoading } =
    useUserSecurityPermission();

  const userRole = normalizeText(
    user?.role ?? user?.UserRole ?? user?.user_role,
  );

  const visibleCards = useMemo(
    () =>
      HOME_ACTION_CARDS.filter(
        (card) =>
          card.roles.includes(userRole) &&
          hasPathAccess(card.accessPath || card.path, menuItems) &&
          hasPermission(card.permission.screen, card.permission.action),
      ),
    [menuItems, userRole, hasPermission],
  );

  if (menuAccessLoading || securityPermissionLoading) {
    return null;
  }

  return (
    <section className="tp-home-actions" aria-label="Timesheet quick actions">
      {visibleCards.length > 0 ? (
        <div className="tp-home-action-grid">
          {visibleCards.map((card) => (
            <button
              key={card.key}
              type="button"
              className={`tp-home-action-card tp-home-action-card-${card.tone}`}
              onClick={() => navigate(card.path)}
              title={card.actionLabel}
              aria-label={card.actionLabel}
            >
              <span className="tp-home-action-card-accent" aria-hidden="true" />
              <span className="tp-home-action-card-orb" aria-hidden="true" />
              <span
                className="tp-home-action-card-orb tp-home-action-card-orb-small"
                aria-hidden="true"
              />

              <span className="tp-home-action-card-header">
                <span className="tp-home-action-icon" aria-hidden="true">
                  <SvgIcon name={card.icon} size={28} strokeWidth={1.75} />
                </span>

                <span className="tp-home-action-eyebrow">{card.eyebrow}</span>
              </span>

              <span className="tp-home-action-content">
                <strong>{card.title}</strong>
                <span>{card.description}</span>
              </span>

              <span className="tp-home-action-card-footer" aria-hidden="true">
                <span className="tp-home-action-link">{card.actionLabel}</span>

                <span className="tp-home-action-arrow">
                  <SvgIcon name="arrowRight" size={18} strokeWidth={2.1} />
                </span>
              </span>
            </button>
          ))}
        </div>
      ) : (
        <div className="tp-home-no-actions">
          <span className="tp-home-no-actions-icon" aria-hidden="true">
            <SvgIcon name="home" size={26} />
          </span>
          <strong>Welcome to TimePro</strong>
          <span>No quick actions are available for your current access.</span>
        </div>
      )}
    </section>
  );
}
