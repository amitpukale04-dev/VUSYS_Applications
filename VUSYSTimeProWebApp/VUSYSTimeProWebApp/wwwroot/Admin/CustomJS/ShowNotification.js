function ShowNotificationMessage(message, title, type, position, timeout) {
    switch (type) {
        case "info":
            toastr.info(message, title, {
                "closeButton": true,
                "debug": false,
                "positionClass": "toast-" + position,
                "onclick": null,
                "progressBar": true,
                "rtl": false,
                "draggable": true,
                "showDuration": "1000",
                "hideDuration": "1000",
                "timeOut": timeout,
                "extendedTimeOut": "1000",
                "showEasing": "swing",
                "hideEasing": "linear",
                "showMethod": "slideDown",
                "hideMethod": "fadeOut",
                "closeMethod": "slideUp",
                "theme": 'colored'
            });
            break;
        case "success":
            toastr.success(message, title, {
                "closeButton": true,
                "debug": false,
                "positionClass": "toast-" + position,
                "onclick": null,
                "progressBar": true,
                "rtl": false,
                "draggable": true,
                "showDuration": "1000",
                "hideDuration": "1000",
                "timeOut": timeout,
                "extendedTimeOut": "1000",
                "showEasing": "swing",
                "hideEasing": "linear",
                "showMethod": "slideDown",
                "hideMethod": "fadeOut",
                "closeMethod": "slideUp",
                "theme": 'colored'
            });
            break;
        case "warning":
            toastr.warning(message, title, {
                "closeButton": true,
                "debug": false,
                "positionClass": "toast-" + position,
                "onclick": null,
                "progressBar": true,
                "rtl": false,
                "draggable": true,
                "showDuration": "1000",
                "hideDuration": "1000",
                "timeOut": timeout,
                "extendedTimeOut": "1000",
                "showEasing": "swing",
                "hideEasing": "linear",
                "showMethod": "slideDown",
                "hideMethod": "fadeOut",
                "closeMethod": "slideUp",
                "theme": 'colored'
            });
            break;
        case "error":
            toastr.error(message, title, {
                "closeButton": true,
                "debug": false,
                "positionClass": "toast-" + position,
                "onclick": null,
                "progressBar": true,
                "rtl": false,
                "draggable": true,
                "showDuration": "1000",
                "hideDuration": "1000",
                "timeOut": timeout,
                "extendedTimeOut": "1000",
                "showEasing": "swing",
                "hideEasing": "linear",
                "showMethod": "slideDown",
                "hideMethod": "fadeOut",
                "closeMethod": "slideUp",
                "theme": 'colored'
            });
            break;
    }
}