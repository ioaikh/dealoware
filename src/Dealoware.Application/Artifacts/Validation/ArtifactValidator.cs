using Dealoware.Application.Artifacts.Dtos;

namespace Dealoware.Application.Artifacts.Validation;

public static class ArtifactValidator
{
    public const int MaxEntities = 50;
    public const int MaxPropertiesPerEntity = 50;
    public const int MaxFactsPerEntity = 100;
    public const int MaxValues = 20;
    public const int MaxLocations = 50;
    public const int MaxTimePeriods = 50;
    public const int MaxStringLength = 4096;
    public const int MaxPropertyStringLength = 1024;

    public static ValidationResult Validate(CreateArtifactRequest request)
    {
        var errors = new List<string>();

        if (request.Entities is null || request.Entities.Count == 0)
        {
            errors.Add("At least one subject entity is required.");
        }
        else
        {
            if (request.Entities.Count > MaxEntities)
            {
                errors.Add($"Maximum {MaxEntities} entities allowed.");
            }

            for (int i = 0; i < request.Entities.Count; i++)
            {
                var entity = request.Entities[i];
                ValidateEntity(entity, i, errors);
            }
        }

        if (string.IsNullOrWhiteSpace(request.Intent))
        {
            errors.Add("Intent is required.");
        }
        else if (request.Intent.Length > MaxStringLength)
        {
            errors.Add($"Intent exceeds maximum length of {MaxStringLength} characters.");
        }

        if (request.Values is not null)
        {
            if (request.Values.Count > MaxValues)
            {
                errors.Add($"Maximum {MaxValues} values allowed.");
            }

            ValidateValues(request.Values, errors);
        }

        if (request.Locations is not null)
        {
            if (request.Locations.Count > MaxLocations)
            {
                errors.Add($"Maximum {MaxLocations} locations allowed.");
            }

            for (int i = 0; i < request.Locations.Count; i++)
            {
                var location = request.Locations[i];
                if (location is not null && location.Length > MaxStringLength)
                {
                    errors.Add($"Location[{i}] exceeds maximum length of {MaxStringLength} characters.");
                }
            }
        }

        if (request.TimePeriods is not null)
        {
            if (request.TimePeriods.Count > MaxTimePeriods)
            {
                errors.Add($"Maximum {MaxTimePeriods} time periods allowed.");
            }

            for (int i = 0; i < request.TimePeriods.Count; i++)
            {
                var period = request.TimePeriods[i];
                if (period.End < period.Start)
                {
                    errors.Add($"TimePeriods[{i}]: End must be >= Start.");
                }
            }
        }

        return new ValidationResult(errors);
    }

    private static void ValidateEntity(CreateSubjectEntityDto entity, int index, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            errors.Add($"Entities[{index}]: Name is required.");
        }
        else if (entity.Name.Length > MaxStringLength)
        {
            errors.Add($"Entities[{index}]: Name exceeds maximum length of {MaxStringLength} characters.");
        }

        if (entity.Description is not null && entity.Description.Length > MaxStringLength)
        {
            errors.Add($"Entities[{index}]: Description exceeds maximum length of {MaxStringLength} characters.");
        }

        if (entity.Properties is not null)
        {
            if (entity.Properties.Count > MaxPropertiesPerEntity)
            {
                errors.Add($"Entities[{index}]: Maximum {MaxPropertiesPerEntity} properties allowed.");
            }

            for (int j = 0; j < entity.Properties.Count; j++)
            {
                var prop = entity.Properties[j];
                ValidateProperty(prop, index, j, errors);
            }
        }

        if (entity.Facts is not null)
        {
            if (entity.Facts.Count > MaxFactsPerEntity)
            {
                errors.Add($"Entities[{index}]: Maximum {MaxFactsPerEntity} facts allowed.");
            }

            for (int j = 0; j < entity.Facts.Count; j++)
            {
                var fact = entity.Facts[j];
                if (fact is not null && fact.Length > MaxStringLength)
                {
                    errors.Add($"Entities[{index}].Facts[{j}] exceeds maximum length of {MaxStringLength} characters.");
                }
            }
        }
    }

    private static void ValidateProperty(CreatePropertyDto prop, int entityIndex, int propIndex, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(prop.Name))
        {
            errors.Add($"Entities[{entityIndex}].Properties[{propIndex}]: Name is required.");
        }
        else if (prop.Name.Length > MaxPropertyStringLength)
        {
            errors.Add($"Entities[{entityIndex}].Properties[{propIndex}]: Name exceeds maximum length of {MaxPropertyStringLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(prop.Type))
        {
            errors.Add($"Entities[{entityIndex}].Properties[{propIndex}]: Type is required.");
        }
        else if (prop.Type.Length > MaxPropertyStringLength)
        {
            errors.Add($"Entities[{entityIndex}].Properties[{propIndex}]: Type exceeds maximum length of {MaxPropertyStringLength} characters.");
        }

        if (prop.Value is not null && prop.Value.Length > MaxPropertyStringLength)
        {
            errors.Add($"Entities[{entityIndex}].Properties[{propIndex}]: Value exceeds maximum length of {MaxPropertyStringLength} characters.");
        }
    }

    private static void ValidateValues(List<CreateValueDto> values, List<string> errors)
    {
        var currencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        for (int i = 0; i < values.Count; i++)
        {
            var value = values[i];
            
            if (string.IsNullOrWhiteSpace(value.Currency))
            {
                errors.Add($"Values[{i}]: Currency is required.");
            }
            else
            {
                var normalizedCurrency = value.Currency.ToUpperInvariant();
                if (!currencies.Add(normalizedCurrency))
                {
                    errors.Add($"Duplicate currency '{value.Currency}' (case-insensitive). At most one value per currency allowed.");
                }
            }
        }
    }
}

public sealed record ValidationResult(List<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
