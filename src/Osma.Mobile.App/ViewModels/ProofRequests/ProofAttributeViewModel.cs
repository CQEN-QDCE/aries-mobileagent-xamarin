namespace Osma.Mobile.App.ViewModels.ProofRequests
{
    public class ProofAttributeViewModel
    {
        public ProofAttributeViewModel()
        {
            Type = "Text";
            IsPredicate = false;
            IsRevealed = true;
        }

        public string Id { get; set; }

        public string CredentialId { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public string Value { get; set; }

        public string Date { get; set; }

        public string FileExt { get; set; }

        public bool IsPredicate { get; set; }

        public bool IsRevealed { get; set; }

    }
}