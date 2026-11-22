function ApproveRejectPopup(element) {
    const ProjectTimeSheetID = element.getAttribute('data-ProjectTimeSheetID');
    const EmployeeCode = element.getAttribute('data-EmployeeCode');
    const TimesheetMonth = element.getAttribute('data-TimesheetMonth');
    const TimesheetYear = element.getAttribute('data-TimesheetYear');
    $('#modalApproveReject').modal('show');
    $('#hdnProjectTimesheetID').val(ProjectTimeSheetID);
    $('#projectTimesheetApproveRejection_EmployeeCode').val(EmployeeCode);
    $('#projectTimesheetApproveRejection_TimesheetMonth').val(TimesheetMonth);
    $('#projectTimesheetApproveRejection_TimesheetYear').val(TimesheetYear);
    $('#txtRemarks').val('');
}

function ReasonPopup(element) {
    const ApprovalStatus = element.getAttribute('data-approvalstatus');
    const ApprovedRejectedUser = element.getAttribute('data-approvedrejecteduser');
    const Reason = element.getAttribute('data-reason');
    const ApprovedRejectedDateTime = element.getAttribute('data-approvedrejecteddatetime');
    $('#modalReason').modal('show');
    $('#modalReasonTitle').text(ApprovalStatus + " Details");
    $('#displayApprovalStatus').text(ApprovalStatus);
    $('#displayApprovedRejectedUserLabel').text(ApprovalStatus + " By:");
    $('#displayApprovedRejectedUser').text(ApprovedRejectedUser);
    $('#displayApprovedRejectedDateTimeLabel').text(ApprovalStatus + " Date & Time:");
    $('#displayApprovedRejectedDateTime').text(ApprovedRejectedDateTime);
    $('#displayReasonLabel').text(ApprovalStatus + " Reason:");
    $('#displayReason').text(Reason);

}

function confirmApproveReject(element) {
    const selectedText = $('#ddlProjectTimesheetApprovalStatus option:selected').text().trim();
    const actionText = selectedText === "Approve" ? "Approve" : "Reject";
    const confirmMessage = `Are You Sure You Want To ${actionText} This Timesheet?`;

    const originalHtml = element.innerHTML;

    $(element).prop('disabled', true);
    $(element).html('<i class="fa fa-spinner fa-spin"></i> Sending...');

    bootbox.confirm({
        title: 'Timesheet Confirmation',
        message: confirmMessage,
        buttons: {
            confirm: {
                label: '<i class="fa fa-check"></i> Yes',
                className: 'btn-success'
            },
            cancel: {
                label: '<i class="fa fa-times"></i> No',
                className: 'btn-danger'
            }
        },
        callback: function (confirmed) {
            if (confirmed) {
                if ($("#GetAllTimesheetApprovalRejectionListForm").valid()) {
                    $('<input>').attr({
                        type: 'hidden',
                        name: $(element).attr('name'),
                        value: $(element).val()
                    }).appendTo('#GetAllTimesheetApprovalRejectionListForm');

                    $("#GetAllTimesheetApprovalRejectionListForm").trigger("submit");
                } else {
                    // Re-enable if validation fails
                    $(element).prop('disabled', false);
                    $(element).html(originalHtml);
                }
            } else {
                $(element).prop('disabled', false);
                $(element).html(originalHtml);
            }
        }
    });

    return false;
}

function DownloadTimesheet(element) {
    $(element).prop('disabled', true);
    $(element).html('<i class="fa fa-spinner fa-spin"></i> Downloading...');
    window.location = element.getAttribute('data-href');

    setTimeout(function () {
        $(element).prop('disabled', false);
        $(element).html('<i class="fas fa-download text-info"></i>');
    }, 1000);
}

function ViewTimeSheetDetails(button) {
    const $btn = $(button);
    const originalHtml = $btn.html();
    // Disable button and show spinner

    $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Loading...');

    const EmployeeCode = $('#projectTimesheetApproveRejection_EmployeeCode').val();
    const ProjectTimeSheetID = $('#hdnProjectTimesheetID').val();
    const TimeSheetMonth = $('#projectTimesheetApproveRejection_TimesheetMonth').val();
    const TimeSheetYear = $('#projectTimesheetApproveRejection_TimesheetYear').val();
    $.ajax({
        url: '/TimesheetApprovalRejection/ViewTimeSheetDetails',
        type: 'GET',
        cache: false,
        dataType: 'json',
        data: { ProjectTimeSheetID: ProjectTimeSheetID, EmployeeCode: EmployeeCode, TimeSheetMonth: TimeSheetMonth, TimeSheetYear: TimeSheetYear },
        success: function (response) {
            if (response.success) {
                const columns = [
                    { data: 'slNo', title: 'SL No', width: '5%' },
                    { data: 'displayTaskDate', title: 'Task Date', width: '10%' },
                    { data: 'clientName', title: 'Client', width: '10%' },
                    { data: 'projectName', title: 'Project', width: '10%' },
                    { data: 'activityName', title: 'Activity', width: '10%' },
                    {
                        data: 'taskDescription', title: 'Task Description', width: '40%',
                        render: function (data, type, row, meta) {
                            return type === 'display' ? data : data;
                        }
                    },
                    { data: 'totalHoursWorked', title: 'No. of Hours Worked', width: '10%' }
                ];
                $('#modalViewTimesheet').modal('show');
                $('#modalViewTimesheet').on('shown.bs.modal', function () {
                    BindCustomDataTableGrid('#tblViewTimeSheetList', response.lstProjectTimesheetDownloadList, columns, true, true, false, false, true);

                    // Adjust column widths and wrapping after render
                    setTimeout(() => {
                        $('#tblViewTimeSheetList').DataTable().columns.adjust().draw();
                    }, 200); // slight delay ensures layout is ready
                });
            }
            else {
                ShowNotificationMessage('Error fetching Timesheet Details.', "VUSYS Time Pro", "error", "center-center", 2000);
            }
        },
        error: function () {
            ShowNotificationMessage('Error fetching Timesheet Details.', "VUSYS Time Pro", "error", "center-center", 2000);
        },
        complete: function () {
            // Re-enable button and restore original content
            $btn.prop('disabled', false).html(originalHtml);
        }
    });
}