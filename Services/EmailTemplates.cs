namespace UrbanDrive.Services
{
    public static class EmailTemplates
    {
        public static (string Subject, string Body) GetTemplate(string templateType, Dictionary<string, string> data)
        {
            return templateType switch
            {
                "Welcome" => GetWelcomeTemplate(data),
                "EmailVerification" => GetEmailVerificationTemplate(data),
                "PasswordReset" => GetPasswordResetTemplate(data),
                "NewBooking" => GetNewBookingTemplate(data),
                "DriverAssignment" => GetDriverAssignmentTemplate(data),
                "BookingConfirmation" => GetBookingConfirmationTemplate(data),
                "PasswordChanged" => GetPasswordChangedTemplate(data),
                _ => throw new ArgumentException($"Unknown template type: {templateType}")
            };
        }

        private static (string Subject, string Body) GetWelcomeTemplate(Dictionary<string, string> data)
        {
            var subject = $"Welcome to UrbanDrive";
            var body = $@"
                <h2>Welcome {data["FullName"]}!</h2>
                <p>Your account has been created in UrbanDrive.</p>
                <p><strong>Your Role:</strong> {data["Role"]}</p>
                <p>Please click the link below to set your password:</p>
                <p><a href='{data["ResetLink"]}'>Set Your Password</a></p>
                <p>This link will expire in 48 hours.</p>
                <br>
                <p>Best regards,<br>UrbanDrive Team</p>";
            return (subject, body);
        }

        private static (string Subject, string Body) GetEmailVerificationTemplate(Dictionary<string, string> data)
        {
            var subject = "Verify Your Email Address";
            var body = $@"
                <h2>Email Verification</h2>
                <p>Dear {data["FullName"]},</p>
                <p>Please click the link below to verify your email address:</p>
                <p><a href='{data["VerificationLink"]}'>Verify Email Address</a></p>
                <p>This link will expire in 24 hours.</p>
                <br>
                <p>Best regards,<br>UrbanDrive Team</p>";
            return (subject, body);
        }

        private static (string Subject, string Body) GetPasswordResetTemplate(Dictionary<string, string> data)
        {
            var subject = "Reset Your Password";
            var body = $@"
                <h2>Password Reset Request</h2>
                <p>Dear {data["FullName"]},</p>
                <p>Click the link below to create a new password:</p>
                <p><a href='{data["ResetLink"]}'>Reset Password</a></p>
                <p>This link will expire in 24 hours.</p>
                <p>If you did not request this, please ignore this email.</p>
                <br>
                <p>Best regards,<br>UrbanDrive Team</p>";
            return (subject, body);
        }

        private static (string Subject, string Body) GetNewBookingTemplate(Dictionary<string, string> data)
        {
            var subject = $"New Vehicle Booking Request #{data["BookingId"]}";
            var body = $@"
                <h2>New Booking Request</h2>
                <p>A new booking requires your approval.</p>
                <p><strong>Booking ID:</strong> {data["BookingId"]}<br>
                <strong>Requester:</strong> {data["RequesterName"]}<br>
                <strong>Destination:</strong> {data["Destination"]}<br>
                <strong>Start Date:</strong> {data["StartDate"]}<br>
                <strong>Purpose:</strong> {data["Purpose"]}</p>
                <p><a href='{data["ApprovalLink"]}'>Review and Approve</a></p>";
            return (subject, body);
        }

        private static (string Subject, string Body) GetDriverAssignmentTemplate(Dictionary<string, string> data)
        {
            var subject = $"New Trip Assignment - {data["Destination"]}";
            var body = $@"
                <h2>New Trip Assignment</h2>
                <p>Dear {data["DriverName"]},</p>
                <p>You have been assigned a new trip.</p>
                <p><strong>Passenger:</strong> {data["PassengerName"]}<br>
                <strong>Passenger Phone:</strong> {data["PassengerPhone"]}<br>
                <strong>Destination:</strong> {data["Destination"]}<br>
                <strong>Pickup Date:</strong> {data["PickupDate"]}<br>
                <strong>Vehicle:</strong> {data["VehicleReg"]}</p>
                <p><a href='{data["DashboardLink"]}'>View Driver Dashboard</a></p>";
            return (subject, body);
        }

        private static (string Subject, string Body) GetBookingConfirmationTemplate(Dictionary<string, string> data)
        {
            var subject = "Your Booking Has Been Approved";
            var body = $@"
                <h2>Booking Approved!</h2>
                <p>Dear {data["PassengerName"]},</p>
                <p>Your booking has been approved and a driver assigned.</p>
                <p><strong>Driver Name:</strong> {data["DriverName"]}<br>
                <strong>Driver Phone:</strong> {data["DriverPhone"]}<br>
                <strong>Vehicle:</strong> {data["VehicleReg"]}<br>
                <strong>Pickup Date:</strong> {data["PickupDate"]}</p>
                <p><a href='{data["TrackingLink"]}'>Track Your Booking</a></p>";
            return (subject, body);
        }

        private static (string Subject, string Body) GetPasswordChangedTemplate(Dictionary<string, string> data)
        {
            var subject = "Your Password Has Been Changed";
            var body = $@"
                <h2>Password Changed</h2>
                <p>Dear {data["FullName"]},</p>
                <p>Your password was changed on {data["ChangeDate"]}.</p>
                <p>If you did not perform this action, please contact your administrator.</p>";
            return (subject, body);
        }
    }
}