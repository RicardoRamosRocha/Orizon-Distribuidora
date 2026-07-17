namespace Orizon.Distribuidora.Domain.Services;

public static class DocumentValidator
{
    public static string Normalize(string? document)
    {
        if (string.IsNullOrWhiteSpace(document))
        {
            return string.Empty;
        }

        return new string(
            document
                .Where(char.IsDigit)
                .ToArray());
    }

    public static bool IsValidCpfOrCnpj(string? document)
    {
        var normalized = Normalize(document);

        return normalized.Length switch
        {
            11 => IsValidCpf(normalized),
            14 => IsValidCnpj(normalized),
            _ => false
        };
    }

    private static bool IsValidCpf(string cpf)
    {
        if (cpf.Distinct().Count() == 1)
        {
            return false;
        }

        var firstDigit = CalculateCpfDigit(cpf[..9], 10);
        var secondDigit = CalculateCpfDigit(cpf[..9] + firstDigit, 11);

        return cpf.EndsWith($"{firstDigit}{secondDigit}", StringComparison.Ordinal);
    }

    private static int CalculateCpfDigit(string value, int weight)
    {
        var sum = 0;

        foreach (var digit in value)
        {
            sum += (digit - '0') * weight--;
        }

        var remainder = sum % 11;

        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static bool IsValidCnpj(string cnpj)
    {
        if (cnpj.Distinct().Count() == 1)
        {
            return false;
        }

        var firstDigit = CalculateCnpjDigit(cnpj[..12], [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        var secondDigit = CalculateCnpjDigit(cnpj[..12] + firstDigit, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);

        return cnpj.EndsWith($"{firstDigit}{secondDigit}", StringComparison.Ordinal);
    }

    private static int CalculateCnpjDigit(string value, int[] weights)
    {
        var sum = 0;

        for (var index = 0; index < value.Length; index++)
        {
            sum += (value[index] - '0') * weights[index];
        }

        var remainder = sum % 11;

        return remainder < 2 ? 0 : 11 - remainder;
    }
}
