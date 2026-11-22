var specialKeys = new Array();
specialKeys.push(8); //Backspace
function IsNumeric(e) {
    var keyCode = e.which ? e.which : e.keyCode
    var ret = ((keyCode >= 48 && keyCode <= 57) || specialKeys.indexOf(keyCode) != -1);
    return ret;
}

function RestrictSpecialCharacters(e) {
    var keyCode = e.which ? e.which : e.keyCode
    var regex = /^[A-Za-z0-9]+$/;
    //Validate TextBox value against the Regex.
    var isValid = regex.test(String.fromCharCode(keyCode));
    var ret = false;
    if (!isValid) {
        ret = false;
    }
    else {
        ret = true;
    }
    return ret;
}

function fnRedirectToHomePage() {
    var baseUrl = localStorage.getItem('VUSYSBaseURL');
    baseUrl = baseUrl + "Home/Home";
    window.location = baseUrl;
}

function fnRedirectToErrorPage() {
    var baseUrl = localStorage.getItem('VUSYSBaseURL');
    baseUrl = baseUrl + "Error/UnExpectedError";
    window.location = baseUrl;
}

function fnRedirectToLoginPage() {
    var baseUrl = localStorage.getItem('VUSYSBaseURL');
    window.location = baseUrl;
}

function fnSaveUpdate(element) {
    const $button = $(element);
    const $form = $button.closest("form");
    const originalHtml = element.innerHTML;

    $button.prop('disabled', true).html('<i class="fa fa-spinner fa-spin"></i> Please Wait...');

    if ($form.valid()) {
        $form.trigger("submit");
    }
    else {
        $button.prop('disabled', false).html(originalHtml);
    }
}

function fnReset(element) {
    const $form = $(element).closest('form'); // Finds the parent form of the clicked button

    if ($form.length === 0) {
        console.warn('No parent form found for reset.');
        return;
    }

    // Clear text-based inputs
    $form.find('input[type="text"], input[type="email"], input[type="tel"], input[type="number"], input[type="password"], input[type="date"]').val('');

    // 📝 Clear textarea fields
    $form.find('textarea').val('');

    // Optional: Clear hidden fields (uncomment if needed)
    // $form.find('input[type="hidden"]').val('');

    // Clear file inputs
    $form.find('input[type="file"]').val('');

    // Uncheck checkboxes and radios
    $form.find('input[type="checkbox"], input[type="radio"]').prop('checked', false);

    // Reset select elements (standard + select2)
    $form.find('select').each(function () {
        const $select = $(this);
        $select.val('');
        if ($select.hasClass('select2-hidden-accessible')) {
            $select.trigger('change'); // Reset select2
        }
    });

    // Reset jQuery DataTables (if any)
    $form.find('.dataTable').each(function () {
        const table = $(this).DataTable();
        table.clear().draw(); // Clear table data
        table.search('').columns().search('').draw(); // Reset search filters
    });

    // Clear validation messages
    $form.find('.field-validation-error, .text-danger').text('');
}

function BindDataTableGrid(selector, EnablePaging, EnableSearching, EnableSorting, EnableInfo) {
    if (!selector) return;

    $(selector).DataTable({
        // Optional customizations:
        paging: EnablePaging,
        searching: EnableSearching,
        ordering: EnableSorting,
        info: EnableInfo,
        // lengthChange: true,
        // pageLength: 10,
        lengthMenu: [[10, 25, 50, -1], [10, 25, 50, 'All']],
        language: {
            emptyTable: "No Record Found.",
            processing:
                '<i class="fa fa-spinner fa-spin fa-3x fa-fw"></i> <span class="sr-only">Loading...</span>'
        }
    });
}

function BindCustomDataTableGrid(tableSelector, data, columnDefs, EnablePaging, EnableSearching, EnableSorting, EnableInfo, EnableScrollX) {
    if ($.fn.DataTable.isDataTable(tableSelector)) {
        $(tableSelector).DataTable().clear().destroy();
    }
    $(tableSelector).DataTable({
        data: data,
        columns: columnDefs,
        paging: EnablePaging,
        searching: EnableSearching,
        ordering: EnableSorting,
        info: EnableInfo,
        scrollX: EnableScrollX,
        lengthMenu: [[10, 25, 50, -1], [10, 25, 50, 'All']],
        language: {
            emptyTable: "No Record Found.",
            processing:
                '<i class="fa fa-spinner fa-spin fa-3x fa-fw"></i> <span class="sr-only">Loading...</span>'
        }
    });
}

