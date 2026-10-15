namespace ShoppingApplication.Model
{
    // One delivery address. Every address has its own phone number.
    public class Address
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
        public string Pincode { get; set; } = "";
        public string Phone { get; set; } = "";

        public override string ToString() => $"{Street}, {City} - {Pincode} | Ph: {Phone}";
    }
}
