$(document).ready(function () {
	setTimeout(function () {
		$('#otpauthsuccess').hide();
	}, 3000);
	setTimeout(function () {
		$('#otpautherror').hide();
	}, 3000);
});

window.onload = function () {
	document.getElementById("Digit1").focus();
}

document.addEventListener('DOMContentLoaded', function () {
	// Get all the textboxes
	const textboxes = document.querySelectorAll('input[type="text"]');

	// Function to validate each textbox
	function validateTextbox(textbox) {
		// Example: Check if the input is numeric and not empty
		const isValid = /^[0-9]*$/.test(textbox.value);  // Only numeric validation

		if (isValid && textbox.value.length === textbox.maxLength) {
			textbox.classList.remove('invalid');  // Remove invalid class if valid
			textbox.classList.add('valid');  // Optionally add a valid class
			return true;
		} else {
			textbox.classList.remove('valid');
			textbox.classList.add('invalid');  // Add invalid class for invalid input
			return false;
		}
	}

	textboxes.forEach((textbox, index) => {
		textbox.addEventListener('input', function () {
			// Validate the current textbox
			if (validateTextbox(textbox)) {
				// If valid, move to the next textbox when the maxlength is reached
				if (textbox.value.length === textbox.maxLength) {
					if (index + 1 < textboxes.length) {
						textboxes[index + 1].focus();
					}
				}
			}
		});

		textbox.addEventListener('keydown', function (e) {
			// Handle Backspace and focus shift for invalid input
			if (e.key === 'Backspace' && textbox.value.length === 0) {
				if (index - 1 >= 0) {
					textboxes[index - 1].focus();
				}
			}
		});
	});
});

function startTimer(durationInSeconds, onTimerEndCallback = null) {
	let timer = durationInSeconds, minutes, seconds;
	const display = document.getElementById('timer');
	display.style.display = 'inline';

	countdown = setInterval(function () {
		minutes = parseInt(timer / 60, 10);
		seconds = parseInt(timer % 60, 10);
		minutes = minutes < 10 ? "0" + minutes : minutes;
		seconds = seconds < 10 ? "0" + seconds : seconds;

		display.textContent = `${minutes}:${seconds}`;

		if (--timer < 0) {
			clearInterval(countdown);
			display.textContent = '';
			$("#resendOtpLink").prop('disabled', false);
			$("#resendOtpLink").html('Resend OTP');
			if (onTimerEndCallback) onTimerEndCallback();
		}
	}, 1000);
}

function ResendOTP(resendUrl, timeoutDurationSeconds, antiForgeryToken) {
	$("#resendOtpLink").prop('disabled', true);
	$("#resendOtpLink").html('<i class="fa fa-spinner fa-spin"></i> Sending...');
	$("#btnVerifyOTP").prop('disabled', true);
	$.ajax({
		url: resendUrl,
		type: 'POST',
		headers: {
			'RequestVerificationToken': antiForgeryToken
		},
		success: function (data) {
			if (data.success) {
				startTimer(timeoutDurationSeconds);
				$("#resendOtpLink").html('Resend OTP');
				$("#btnVerifyOTP").prop('disabled', false);
				ShowOTPNotificationMessage(data.message || 'OTP resent successfully!', "VUSYS Time Pro", "success", "center-center", "2000");
			} else {
				$("#resendOtpLink").prop('disabled', false);
				$("#resendOtpLink").html('Resend OTP');
				ShowOTPNotificationMessage(data.message || 'Failed to resend OTP.', "VUSYS Time Pro", "error", "center-center", "2000");
			}
		},
		error: function (xhr) {
			$("#resendOtpLink").prop('disabled', false);
			$("#resendOtpLink").html('Resend OTP');
			ShowOTPNotificationMessage(xhr.responseText || 'Unexpected error occurred.', "VUSYS Time Pro", "error", "center-center", "2000");
		}
	});
}

function ShowOTPNotificationMessage(message, title, type, position, timeout) {
	switch (type) {
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

