using Osma.Mobile.App.ViewModels.Credentials;
using System;
using Xamarin.Forms;

namespace Osma.Mobile.App.Views.Credentials
{
    public enum CredentialAttributeType
    {
        None,
        Text = 1,
        Image = 2,
        File = 3,
    }

    public class CredentialAttributeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }

        public DataTemplate ImageTemplate { get; set; }

        public DataTemplate FileTemplate { get; set; }

        public DataTemplate ErrorTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is null) return ErrorTemplate;

            CredentialAttributeType credentialAttributeType;

            var credentialAttribute = item as CredentialAttribute;

            if (credentialAttribute is null)
            {
                return ErrorTemplate;
            }

            try
            {
                credentialAttributeType = (CredentialAttributeType)Enum.Parse(typeof(CredentialAttributeType), credentialAttribute.Type, true);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("Credential Attribute Type is Invalid");
            }
            switch (credentialAttributeType)
            {
                case CredentialAttributeType.Text:
                    return TextTemplate;

                case CredentialAttributeType.Image:
                    return ImageTemplate;

                case CredentialAttributeType.File:
                    return FileTemplate;

                default:
                    return ErrorTemplate;
            }
        }
    }
}