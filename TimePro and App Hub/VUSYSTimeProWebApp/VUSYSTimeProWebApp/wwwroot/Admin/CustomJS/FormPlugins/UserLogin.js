window.addEventListener('load', () => {
    // Simulate a delay for splash screen, in case you want to keep it visible longer
    setTimeout(() => {
        document.getElementById('splash-screen').style.display = 'none';
        if ($('.mainDisplay').css('display') === 'none') {
            $('#maincontent').removeClass('mainDisplay');
        }
    }, 3000); // Hide after 3 seconds (adjust the time as necessary)
});

function fnClearLocalStorage() {
    if (localStorage.getItem('BaseURL') != null) {
        localStorage.removeItem('BaseURL')
    }
}

$(document).ready(function () {
    setTimeout(function () {
        $('#loginerror').hide();
    }, 3000);

    setTimeout(function () {
        $('#changepassworderror').hide();
    }, 3000);

    setTimeout(function () {
        $('#forgotpassworderror').hide();
    }, 3000);

    fnClearLocalStorage();

    $('#LoginForm').on('submit', function () {
        if ($('#LoginForm').valid() == true) {
            $("#btnLogin").prop("disabled", true);
            $("#btnLogin").html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Please wait...');
        }
    });

    $('#ForgotPasswordForm').on('submit', function () {
        if ($('#ForgotPasswordForm').valid() == true) {
            $("#btnSendRequest").prop("disabled", true);
            $("#btnSendRequest").html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Please wait...');
        }
    });

    $("#showhide").click(function () {
        if ($(this).data('val') == "1") {
            $("#txtPassword").prop('type', 'text');
            $("#eye").attr("class", "fa fa-eye-slash");
            $(this).data('val', '0');
        }
        else {
            $("#txtPassword").prop('type', 'password');
            $("#eye").attr("class", "fa fa-eye");
            $(this).data('val', '1');
        }
    });

    //Login Button
    $('#btnLogin').click(function () {
        $('#LoginForm').validate({
            rules: {
                "UserLogin.UserEmailID": {
                    required: true
                },
                "UserLogin.Password": {
                    required: true
                },
            },
            messages: {
                "UserLogin.UserEmailID": {
                    required: "Enter The Email ID."
                },
                "UserLogin.Password": {
                    required: "Enter The Password."
                },
            },
            errorElement: 'span',
            errorPlacement: function (error, element) {
                if (element.parent('.input-group').length) {
                    error.addClass('invalid-feedback');
                    error.insertAfter(element.parent());
                } else {
                    error.insertAfter(element);
                    error.addClass('invalid-feedback');
                }
            },
            onError: function () {
                $('.input-group.error-class').find('.help-block.form-error').each(function () {
                    $(this).closest('.form-group').addClass('error-class').append($(this));
                });
            },
            highlight: function (element, errorClass, validClass) {
                $(element).addClass('is-invalid');
            },
            unhighlight: function (element, errorClass, validClass) {
                $(element).removeClass('is-invalid');
            },
        });
    });

    //Forgot Password Button
    $('#btnSendRequest').click(function () {
        $('#ForgotPasswordForm').validate({
            rules: {
                "forgotPassword.UserEmailID": {
                    required: true
                },
            },
            messages: {
                "forgotPassword.UserEmailID": {
                    required: "Enter The Email ID."
                },
            },
            errorElement: 'span',
            errorPlacement: function (error, element) {
                if (element.parent('.input-group').length) {
                    error.addClass('invalid-feedback');
                    error.insertAfter(element.parent());
                } else {
                    error.insertAfter(element);
                    error.addClass('invalid-feedback');
                }
            },
            onError: function () {
                $('.input-group.error-class').find('.help-block.form-error').each(function () {
                    $(this).closest('.form-group').addClass('error-class').append($(this));
                });
            },
            highlight: function (element, errorClass, validClass) {
                $(element).addClass('is-invalid');
            },
            unhighlight: function (element, errorClass, validClass) {
                $(element).removeClass('is-invalid');
            },
        });
    });

    //ChangePasswordOnReset Button
    $('#btnChangePasswordOnReset').click(function () {
        $('#ChangePasswordOnResetForm').validate({
            rules: {
                "changePasswordOnReset.NewPassword": {
                    required: true
                },
                "changePasswordOnReset.ConfirmPassword": {
                    required: true,
                    equalTo: '[name="changePasswordOnReset.NewPassword"]'
                },
            },
            messages: {
                "changePasswordOnReset.NewPassword": {
                    required: "Enter The New Password."
                },
                "changePasswordOnReset.ConfirmPassword": {
                    required: "Enter The Confirm Password."
                },
            },
            errorElement: 'span',
            errorPlacement: function (error, element) {
                if (element.parent('.input-group').length) {
                    error.addClass('invalid-feedback');
                    error.insertAfter(element.parent());
                } else {
                    error.insertAfter(element);
                    error.addClass('invalid-feedback');
                }
            },
            onError: function () {
                $('.input-group.error-class').find('.help-block.form-error').each(function () {
                    $(this).closest('.form-group').addClass('error-class').append($(this));
                });
            },
            highlight: function (element, errorClass, validClass) {
                $(element).addClass('is-invalid');
            },
            unhighlight: function (element, errorClass, validClass) {
                $(element).removeClass('is-invalid');
            },
        });
    });

    $("#NewPasswordShowHide").click(function () {
        if ($(this).data('val') == "1") {
            $("#txtNewPassword").prop('type', 'text');
            $("#NewPasswordEye").attr("class", "fa fa-eye-slash");
            $(this).data('val', '0');
        }
        else {
            $("#txtNewPassword").prop('type', 'password');
            $("#NewPasswordEye").attr("class", "fa fa-eye");
            $(this).data('val', '1');
        }
    });

    $("#ConfirmPasswordShowHide").click(function () {
        if ($(this).data('val') == "1") {
            $("#txtConfirmPassword").prop('type', 'text');
            $("#ConfirmPasswordEye").attr("class", "fa fa-eye-slash");
            $(this).data('val', '0');
        }
        else {
            $("#txtConfirmPassword").prop('type', 'password');
            $("#ConfirmPasswordEye").attr("class", "fa fa-eye");
            $(this).data('val', '1');
        }
    });

});

