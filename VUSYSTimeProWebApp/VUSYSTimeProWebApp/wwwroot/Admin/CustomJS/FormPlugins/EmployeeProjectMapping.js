$(function () {
	$("#ddlProjectMappingEmployeeCode").select2();
	$("#ddlProjectMappingProjectList").select2();
	

	handleEmployeeProjectChange();
});

function handleEmployeeProjectChange() {
    var EditEmployeeProjectMappingID = $('#employeeProjectMapping_Id').val();
    var selectedProjectId = $('#ddlProjectMappingProjectList').val();
    var selectedProjectText = $('#ddlProjectMappingProjectList option:selected').text().trim().toLowerCase();

    var $notifyCheckbox = $('#employeeProjectMapping_IsNotifyToEmployee');

    if (selectedProjectText === "timeaway") {
        $notifyCheckbox.prop('checked', false).prop('readonly', true);
        $notifyCheckbox.on('click', function (e) { e.preventDefault(); });
    } else {
        // Don't explicitly re-enable
        if ((parseInt(selectedProjectId) > 0 && parseInt(EditEmployeeProjectMappingID) > 0) && EditEmployeeProjectMappingID !== undefined) {
            $notifyCheckbox.prop('readonly', false); // This will enable the checkbox
            // Optionally, you can also check the checkbox if you want
            $notifyCheckbox.prop('checked', false);
            $notifyCheckbox.on('click', function (e) { e.preventDefault(); });
        } else {
            $notifyCheckbox.prop('readonly', true);
            $notifyCheckbox.prop('checked', true);
            $notifyCheckbox.on('click', function (e) { e.preventDefault(); });
        }
    }
}

