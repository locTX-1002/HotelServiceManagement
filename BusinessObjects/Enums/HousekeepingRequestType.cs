namespace BusinessObjects.Enums
{
    public enum HousekeepingRequestType
    {
        // Other = 0 (CLR default) trung voi DB default ben duoi - tranh EF hieu nham "chua gan gia
        // tri" cho request Cleaning (neu Cleaning = 0 thi EF se lang le doi thanh Other luc insert).
        Other,
        Cleaning,
        ExtraTowels,
        ExtraWater
    }
}
