$(function () {
	//$("#ddlProject").select2();
	//$("#ddlProjectActivity").select2();
	$('#txtTaskDescription').summernote({
		height: 150,
		// toolbar: [
		// 	['font', ['bold', 'italic', 'underline', 'clear']], // Font options without font family
		// 	['para', ['ul', 'ol', 'paragraph']], // Paragraph options
		// 	['insert', ['link', 'picture', 'video']] // Insert options
		// ]
		//callbacks: {
		//	onInit: function () {
		//		$('#txtTaskDescription').summernote('code', '<ul><li></li></ul>');
		//	}
		//},
		toolbar: [
			// Font options
			['font', []],
			// Style options
			['style', []],
			// Paragraph options
			['para', ['ul', 'ol']],
			// Insert options (excluding Link, Picture, and Video)
			['insert', []],  // Empty because you want to remove insert options
			// Miscellaneous (excluding Fullscreen, Code View, Help)
			['misc', []]  // Empty to remove those buttons
		]
	});

	handleProjectChange();

	$('#btnSaveTimesheet').click(function () {
		// Disable validation by setting the form attribute
		$('form').data('validator', null);
	});
});

function confirmDeleteTimeSheet(element) {
	bootbox.confirm({
		title: 'Delete Timesheet',
		message: "Are You Sure You Want To Delete Selected Timesheet ?",
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
				window.location = element.getAttribute('href');
			}
		}
	});
	return false; // Prevent the default action
}

function confirmSaveTimesheet(element) {
	const confirmMessage = `Are You Sure You Want To Save This Timesheet?`;

	const $button = $(element);
	const $form = $button.closest("form");
	const originalHtml = element.innerHTML;
	$button.prop('disabled', true).html('<i class="fa fa-spinner fa-spin"></i> Please Wait...');

	bootbox.confirm({
		title: 'Save Timesheet',
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
				if ($form.valid()) {
					$('<input>').attr({
						type: 'hidden',
						name: $(element).attr('name'),
						value: $(element).val()
					}).appendTo($form);

					$form.trigger("submit");
				}
				else {
					$button.prop('disabled', false).html(originalHtml);
				}
			} else {
				$(element).prop('disabled', false);
				$(element).html(originalHtml);
			}
		}
	});
	return false;
}

$('#ddlProject').change(function () {
	handleProjectChange(); // On change of project selection
	var selectedProjectId = $('#ddlProject').val();
	$('#ProjectID').val(selectedProjectId).attr('value', selectedProjectId);
});

$('#ddlProjectActivity').change(function () {
	var selectedProjectActivityId = $('#ddlProjectActivity').val();
	$('#projectTimesheet_ProjectActivityID').val(selectedProjectActivityId);
	var selectedProjectText = $('#ddlProject option:selected').text().trim();
	if (selectedProjectText.toLowerCase() === "timeaway") {
		setOnLeaveMode();
	}
});

function handleProjectChange() {
	var selectedProjectId = $('#ddlProject').val();
	var EditProjectTimesheetID = $('#projectTimesheet_ProjectTimeSheetID').val();
	var EditProjectTimeSheetDetailID = $('#projectTimesheet_ProjectTimeSheetDetailID').val();
	var selectedProjectText = $('#ddlProject option:selected').text().trim();
	if (selectedProjectText.toLowerCase() === "timeaway") {
		setOnLeaveMode();
	}
	else if (selectedProjectId == '') {
		// No project selected (blank)
		resetTimesheetFields();
	}
	else {
		if ((parseInt(selectedProjectId) > 0 && parseInt(EditProjectTimesheetID) > 0) && EditProjectTimesheetID !== undefined) {
			if (selectedProjectText.toLowerCase() === "timeaway") {
				setOnLeaveMode();
			}
			else {
				//make the ajax call
				getProjectTimeSheetDetails(parseInt(EditProjectTimeSheetDetailID));
			}
		}
		else {
			resetTimesheetFields();
		}
	}
}

function getLeaveActivityAndFill(IsOnLeave, selectedProjectActivityId = 0) {
	$.ajax({
		url: '/Timesheet/GetLeaveActivity', // Change to your correct controller/route
		type: 'GET',
		data: { IsOnLeave: IsOnLeave },
		success: function (response) {
			$('#ddlProjectActivity').empty();
			$('#ddlProjectActivity').append('<option value="">SELECT</option>');
			$.each(response, function (i, activity) {
				$('#ddlProjectActivity').append('<option value="' + activity.id + '">' + activity.activityName + '</option>');
			});
			if (selectedProjectActivityId > 0) {
				$('#ddlProjectActivity').val(selectedProjectActivityId)
			}
		},
		error: function () {
			ShowNotificationMessage('Error fetching Leave Activity from server.', "VUSYS Time Pro", "error", "center-center", 2000);
		}
	});
}

function getProjectTimeSheetDetails(ProjectTimesheetDetailID) {
	$.ajax({
		url: '/Timesheet/GetProjectTimesheetDetails',
		type: 'GET',
		data: { ProjectTimesheetDetailID: ProjectTimesheetDetailID },
		success: function (response) {
			if (response != null) {
				resetTimesheetFields();
				$('#projectTimesheet_ProjectTimeSheetID').val(response.projectTimeSheetID);
				$('#projectTimesheet_ProjectTimeSheetDetailID').val(response.projectTimeSheetDetailID);
				$('#ddlProjectActivity').val(response.projectActivityID);
				$('#projectTimesheet_TaskDate').val(formatToYYYYMMDD(response.taskDate));
				$('#projectTimesheet_NoofHoursWorked').val(response.noofHoursWorked);
				$('#txtTaskDescription').summernote('code', response.taskDescriptionRaw);
				$('#chkIsOnLeave').prop('checked', false).prop('disabled', false).off('click').on('click', function (e) { e.preventDefault(); });
			}
			else {
				ShowNotificationMessage('Unable to fetch Leave Activity. Please contact Admin.', "VUSYS Time Pro", "error", "center-center", 2000);
			}
		},
		error: function () {
			ShowNotificationMessage('Error fetching Leave Activity from server.', "VUSYS Time Pro", "error", "center-center", 2000);
		}
	});
}

function resetTimesheetFields() {
	var TaskDescription = $('#txtTaskDescription').summernote('code');
	var ProjectID = $('#ddlProject').val();
	var NoofHoursWorked = $('#projectTimesheet_NoofHoursWorked').val();
	if (TaskDescription != '' && TaskDescription != '<p><br></p>') {
		if (ProjectID !== '') {
			$('#txtTaskDescription').summernote('code', TaskDescription);
		}
		else {
			$('#txtTaskDescription').summernote('code', '');
		}
	}
	else {
		/*$('#txtTaskDescription').summernote('code', '');*/
		if (ProjectID !== '') {
			$('#txtTaskDescription').summernote('code', '<ol><li></li></ol>');
			// Move cursor inside the first <li>
			let editor = $('#txtTaskDescription').next('.note-editor').find('.note-editable')[0];
			let range = document.createRange();
			let sel = window.getSelection();
			range.setStart(editor.querySelector('li'), 0);
			range.collapse(true);
			sel.removeAllRanges();
			sel.addRange(range);
		}
		else {
			$('#txtTaskDescription').summernote('code', '');
		}
	}
	$('#txtTaskDescription').summernote('enable');
	var selectedProjectActivity = $('#ddlProjectActivity').val();
	getLeaveActivityAndFill(false, selectedProjectActivity);
	if (selectedProjectActivity == '') {
		$('#ddlProjectActivity').prop('disabled', false);
		$('#ddlProjectActivity').val('');
	}
	$('#chkIsOnLeave').prop('checked', false).prop('disabled', false).off('click').on('click', function (e) { e.preventDefault(); });
	if (ProjectID !== '') {
		$('#projectTimesheet_NoofHoursWorked').val(NoofHoursWorked);
	}
	else {
		$('#projectTimesheet_NoofHoursWorked').val('');
	}
	$('#ddlProject').prop('disabled', false);
}

function formatToYYYYMMDD(isoString) {
	if (!isoString) return '';

	const date = new Date(isoString);
	if (isNaN(date)) return '';
	const year = date.getFullYear();
	const month = ('0' + (date.getMonth() + 1)).slice(-2); // months are 0-based
	const day = ('0' + date.getDate()).slice(-2);
	return `${year}-${month}-${day}`;
}

function setOnLeaveMode() {
	var Description = '';
	var selectedProjectActivityId = $('#ddlProjectActivity').val();
	var NoofHoursWorked = $('#projectTimesheet_NoofHoursWorked').val();
	$('#chkIsOnLeave').prop('checked', true).prop('disabled', false)
		.off('click').on('click', function (e) { e.preventDefault(); });
	if (NoofHoursWorked !== '') {
		$('#projectTimesheet_NoofHoursWorked').val(NoofHoursWorked);
	}
	else {
		$('#projectTimesheet_NoofHoursWorked').val('');
	}
	var SelectedActivity = $('#ddlProjectActivity option:selected').text().trim();
	if (SelectedActivity.toLowerCase() === "on leave") {
		Description = 'On Leave';
		$('#txtTaskDescription').summernote('code', '<ol><li>' + Description + '</li></ol>');
	}
	else if (SelectedActivity.toLowerCase() === "holiday") {
		Description = 'Holiday';
		$('#txtTaskDescription').summernote('code', '<ol><li>' + Description + '</li></ol>');
	}
	else if (SelectedActivity.toLowerCase() === "compoff") {
		Description = 'Comp off';
		$('#txtTaskDescription').summernote('code', '<ol><li>' + Description + '</li></ol>');
	}
	else {
		$('#txtTaskDescription').summernote('code', '');
	}
	$('#txtTaskDescription').summernote('disable');
	if (selectedProjectActivityId !== '') {
		getLeaveActivityAndFill(true, selectedProjectActivityId);
	}
	else {
		getLeaveActivityAndFill(true);
	}
}

function ViewDownloadTimeSheetDetails(button) {
	const $btn = $(button);
	const originalHtml = $btn.html();
	// Disable button and show spinner
	$btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Loading...');
	if ($("#DownloadTimesheetForm").valid()) {
		const EmployeeCode = $('#ddlDownloadTimesheetEmployee').val();
		const TimeSheetMonth = $('#ddlDownloadTimesheetMonth').val();
		const TimeSheetYear = $('#ddlDownloadTimesheetYear').val();

		$.ajax({
			url: '/Timesheet/ViewTimeSheetDetails',
			type: 'GET',
			cache: false,
			dataType: 'json',
			data: { EmployeeCode: EmployeeCode, TimeSheetMonth: TimeSheetMonth, TimeSheetYear: TimeSheetYear },
			success: function (response) {
				if (response.success) {
					const columns = [
						{ data: 'slNo', title: 'SL No', width: '5%' },
						{ data: 'employeeCode', title: 'Employee Code', width: '10%' },
						{ data: 'name', title: 'Name', width: '10%' },
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
						$btn.prop('disabled', false).html(originalHtml);
					});
				}
				else {
					ShowNotificationMessage('Error fetching Timesheet Details.', "VUSYS Time Pro", "error", "center-center", 2000);
					$btn.prop('disabled', false).html(originalHtml);
				}
			},
			error: function () {
				ShowNotificationMessage('Error fetching Timesheet Details.', "VUSYS Time Pro", "error", "center-center", 2000);
				$btn.prop('disabled', false).html(originalHtml);
			},
			complete: function () {
				// Re-enable button and restore original content
				$btn.prop('disabled', false).html(originalHtml);
			}
		});
	}
	else {
		$btn.prop('disabled', false).html(originalHtml);
		$btn.prop('disabled', false).html(originalHtml);
	}
}