using System;

namespace Osma.Mobile.App.ViewModels.Home
{
    public class Notification
    {
        public string RecordId { get; set; }

        public string AgentName { get; set; }

        public string RecordName { get; set; }

        public string Message { get => GetMessage(); }

        public string Icon { get => GetIcon(); }

        public string MessageParameter { get => GetMessageParameter(); }

        public DateTime? DateTime { get; set; }

        public string Date { get => GetDate(DateTime); }

        public string Time { get => GetTime(DateTime); }

        public NotificationType Type { get; set; }

        public NotificationState State { get; set; }

        private string GetIcon()
        {
            switch (Type)
            {
                case NotificationType.Connection:
                    return AppResources.FaNetworkWired;
                case NotificationType.Credential:
                    return AppResources.FaIdCard;
                case NotificationType.ProofRequest:
                    return AppResources.FaQrCode;
            }
            throw new Exception("Invalid message.");
        }
        private string GetMessage()
        {
            switch (Type)
            {
                case NotificationType.Connection:
                    switch (State)
                    {
                        case NotificationState.Invited:
                            return AppResources.InvitedToConnectNotificationMessage;
                        case NotificationState.Negotiating:
                            return AppResources.NegotiatingNotificationMessage;
                        case NotificationState.Connected:
                            return AppResources.ConnectedWithNotificationMessage;
                    }
                    break;
                case NotificationType.Credential:
                    switch (State)
                    {
                        case NotificationState.Offered:
                            return AppResources.OfferedCredentialNotificationMessage;
                        case NotificationState.Requested:
                            return AppResources.RequestedNotificationMessage;
                        case NotificationState.Issued:
                            return AppResources.IssuedCredentialNotificationMessage;
                        case NotificationState.Rejected:
                            return AppResources.RejectedNotificationMessage;
                        case NotificationState.Revoked:
                            return "{Revoked}";
                    }
                    break;
                case NotificationType.ProofRequest:
                    switch (State)
                    {
                        case NotificationState.Proposed:
                            return "{Revoked}";
                        case NotificationState.Requested:
                            return AppResources.RequestedProofNotificationMessage;
                        case NotificationState.Accepted:
                            return AppResources.SharedNotificationMessage;
                        case NotificationState.Rejected:
                            return AppResources.RejectedNotificationMessage;
                    }
                    break;
            }
            throw new Exception("Invalid message.");
        }

        private string GetMessageParameter()
        {
            if (Type == NotificationType.Connection)
            {
                return AgentName;
            } else
            {
                return RecordName;
            }
        }

        private string GetDate(DateTime? value)
        {
            if (!value.HasValue) return string.Empty;
            //string date = value.Value.ToString().Split(' ')[0];
            string date = value.Value.Day.ToString() + " ";
            switch(value.Value.Month)
            {
                case 1:
                    date += "Jan";
                    break;
                case 2:
                    date += "Fév";
                    break;
                case 3:
                    date += "Mar";
                    break;
                case 4:
                    date += "Avr";
                    break;
                case 5:
                    date += "Mai";
                    break;
                case 6:
                    date += "Jui";
                    break;
                case 7:
                    date += "Jui";
                    break;
                case 8:
                    date += "Aou";
                    break;
                case 9:
                    date += "Sep";
                    break;
                case 10:
                    date += "Oct";
                    break;
                case 11:
                    date += "Nov";
                    break;
                case 12:
                    date += "Déc";
                    break;
            }
            date += " " + value.Value.Year;
            return date;
        }

        private string GetTime(DateTime? value)
        {
            if (!value.HasValue) return string.Empty;
            string time = value.Value.ToString().Split(' ')[1];
            time = time.Split(':')[0] + "h" + time.Split(':')[1];
            return time;
        }

        private string FormatDateTime(DateTime? value)
        {
            if (!value.HasValue) return string.Empty;

            string date = value.Value.ToString().Split(' ')[0];
            string time = value.Value.ToString().Split(' ')[1];
            time = time.Split(':')[0] + ":" + time.Split(':')[1];
            return date + " " + time;
        }
    }
}
