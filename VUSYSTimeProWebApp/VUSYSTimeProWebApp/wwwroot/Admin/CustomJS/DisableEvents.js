$(function () {
    if (window.history.replaceState) {
        window.history.replaceState(null, null, window.location.href);
    }
    document.onkeydown = KeyPress;
    function KeyPress(e) {
        var press = window.event ? event : e
        if ((press.keyCode == 82 || press.keyCode == 85) && press.ctrlKey) {
            DisableKeyPressing(e);
        }
        if ((e.which || e.keyCode) == 116) {
            DisableKeyPressing(e);
        }
        if (e.ctrlKey && e.shiftKey && e.keyCode == 73) {
            DisableKeyPressing(e);
        }
    }

});

function DisableKeyPressing(e) {

    // keycodes table https://css-tricks.com/snippets/javascript/javascript-keycodes/
    var conditions = [
        // Diable F5
        (e.which || e.keyCode) == 116,
        // Diable Ctrl+R
        e.ctrlKey && (e.which === 82),

        //Disable Ctrl+Shift+R
        e.ctrlKey && e.shiftKey && e.keyCode == 73,

        //Disable Ctrl+U
        e.ctrlKey && (e.which === 85),
    ]

    if ($.each(conditions, function (key, val) { val + ' || ' })) {
        e.preventDefault();
    }
}