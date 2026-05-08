namespace BloodBankAccounting.Models;

public enum CustomerType
{
    Hospital = 1,
    Person   = 2
}

public enum BloodGroup
{
    APositive  = 1,
    ANegative  = 2,
    BPositive  = 3,
    BNegative  = 4,
    ABPositive = 5,
    ABNegative = 6,
    OPositive  = 7,
    ONegative  = 8
}

public enum PaymentType
{
    Cash = 1,
    Credit = 2,
    Partial = 3   
}

public enum SafeType
{
    Cash = 1,
    Bank = 2
}

public enum BloodBagStatus
{
    Available = 1,
    Sold      = 2,
    Expired   = 3,
    Damaged   = 4,
    Reserved  = 5
}

public enum VoucherType
{
    Receipt = 1,
    Payment = 2
}
