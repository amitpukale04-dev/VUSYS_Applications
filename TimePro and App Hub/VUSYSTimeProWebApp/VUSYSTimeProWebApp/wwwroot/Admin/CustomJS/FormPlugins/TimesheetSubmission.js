function confirmSendForApproval(element) {
    $(element).prop('disabled', true);
    $(element).html('<i class="fa fa-spinner fa-spin"></i> Sending...');
    const buttonText = element.getAttribute('data-buttonText');

    bootbox.confirm({
        title: 'Send For Approval',
        message: "Are You Sure You Want To Send Timesheet For Approval ?",
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
                window.location = element.getAttribute('data-href');
            }
            else {
                // Re-enable the button and restore original text if cancelled
                $(element).prop('disabled', false);
                $(element).html(buttonText); // Restore original text
            }
        }
    });
    return false; // Prevent the default action
}

function confirmSendNotificationToEmployee(element) {
    $(element).prop('disabled', true);
    $(element).html('<i class="fa fa-spinner fa-spin"></i> Sending...');
    const buttonText = element.getAttribute('data-buttonText');

    bootbox.confirm({
        title: 'Send Timesheet Submission Notification',
        message: "Are You Sure You Want To Send Timesheet Submission Notification ?",
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
                window.location = element.getAttribute('data-href');
            }
            else {
                // Re-enable the button and restore original text if cancelled
                $(element).prop('disabled', false);
                $(element).html(buttonText); // Restore original text
            }
        }
    });
    return false; // Prevent the default action
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

function DownloadTimesheet(element) {
    $(element).prop('disabled', true);
    $(element).html('<i class="fa fa-spinner fa-spin"></i> Downloading...');
    window.location = element.getAttribute('data-href');

    setTimeout(function () {
        $(element).prop('disabled', false);
        $(element).html('<i class="fas fa-download text-info"></i>');
    }, 1000);
}