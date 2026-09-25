// NOTE: ISymptomCheckerService is intentionally NOT in Core.Interfaces because its
// parameter and return types (SymptomAssessmentInput, SymptomAssessmentResult) are
// Application-layer DTOs that Core cannot reference without creating a circular dependency.
// The interface is defined in LarvaX.Application.Services.SymptomCheckerService.cs.
// This file is kept as documentation only.
namespace LarvaX.Core.Interfaces
{
    // See LarvaX.Application.Services.ISymptomCheckerService
}
