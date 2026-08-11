namespace FishFarmManager.Services;

public static class BusinessRuleError
{
    private const string DataKey = "FishFarmManager.BusinessRuleCode";

    public static InvalidOperationException Create(string code, string diagnosticMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var exception = new InvalidOperationException(diagnosticMessage);
        exception.Data[DataKey] = code;
        return exception;
    }

    public static bool TryGetCode(Exception exception, out string code)
    {
        code = Convert.ToString(exception.Data[DataKey]) ?? string.Empty;
        return !string.IsNullOrWhiteSpace(code);
    }
}
