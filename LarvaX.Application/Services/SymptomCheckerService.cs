using LarvaX.Application.Models;

namespace LarvaX.Application.Services
{
    public interface ISymptomCheckerService
    {
        SymptomAssessmentResult Assess(SymptomAssessmentInput input);
    }

    public class SymptomCheckerService : ISymptomCheckerService
    {
        public SymptomAssessmentResult Assess(SymptomAssessmentInput input)
        {
            int score = 0;
            var flags = new List<string>();

            if (input.Fever)
            {
                score += 30;
                flags.Add("High fever");
            }
            if (input.SeverHeadache)
            {
                score += 20;
                flags.Add("Severe retro-orbital or frontal headache");
            }
            if (input.RashBehindEyes)
            {
                score += 20;
                flags.Add("Pain behind eyes / macular rash");
            }
            if (input.JointPain)
            {
                score += 10;
                flags.Add("Breakbone joint/muscle aches");
            }
            if (input.Bleeding)
            {
                score += 40; // Critical warning sign
                flags.Add("Mucosal/subcutaneous bleeding signs");
            }
            if (input.VomitingNausea)
            {
                score += 10;
                flags.Add("Persistent vomiting or nausea");
            }
            if (input.AbdominalPain)
            {
                score += 20;
                flags.Add("Severe abdominal tenderness");
            }

            string riskLevel;
            string advice;
            string confidenceNote;
            bool isEmergency = false;

            if (score >= 80 || input.Bleeding)
            {
                riskLevel = "Emergency";
                advice = "You may be experiencing Dengue Hemorrhagic Fever (DHF) or Dengue Shock Syndrome. Seek IMMEDIATE medical care at the nearest emergency room or hospital.";
                confidenceNote = "Model confidence: High — Critical clinical warning signs detected.";
                isEmergency = true;
            }
            else if (score >= 50)
            {
                riskLevel = "High";
                advice = "Your symptoms suggest a high probability of acute dengue infection. Please visit a clinic or hospital today for an NS1 antigen / CBC blood test.";
                confidenceNote = "Model confidence: High — based on 1,250+ validated symptomatic cases.";
            }
            else if (score >= 25)
            {
                riskLevel = "Medium";
                advice = "Some classic dengue symptoms are present. Rest adequately, maintain oral hydration with ORS/fluids, and monitor your temperature. If fever persists >2 days, consult a physician.";
                confidenceNote = "Model confidence: Medium — based on validated symptom correlations.";
            }
            else
            {
                riskLevel = "Low";
                advice = "Symptoms are mild or minimal. Keep monitoring your health. Avoid mosquito bites by using repellents and nets. Seek medical care if high fever or warning signs develop.";
                confidenceNote = "Model confidence: Medium — minimal dengue markers detected.";
            }

            return new SymptomAssessmentResult
            {
                Score = score,
                RiskLevel = riskLevel,
                Advice = advice,
                ConfidenceNote = confidenceNote,
                IsEmergency = isEmergency,
                WarningFlags = flags
            };
        }
    }
}
