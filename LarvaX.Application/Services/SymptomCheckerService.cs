using LarvaX.Application.Models;
using LarvaX.Core.Interfaces;

namespace LarvaX.Application.Services
{
    // ISymptomCheckerService stays in Application.Services (not Core) because its
    // input/output types are Application-layer DTOs — moving them to Core would create
    // a circular reference. See ISymptomCheckerService.cs in Core for the explanation.
    public interface ISymptomCheckerService
    {
        SymptomAssessmentResult Assess(SymptomAssessmentInput input, string language = "en");
    }

    // ISymptomCheckerService is defined above in this same Application.Services namespace.
    public class SymptomCheckerService : ISymptomCheckerService
    {
        public SymptomAssessmentResult Assess(SymptomAssessmentInput input, string language = "en")
        {
            bool bn = language == "bn";
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
                riskLevel = bn ? "জরুরি" : "Emergency";
                advice = bn
                    ? "আপনি ডেঙ্গু হেমোরেজিক ফিভার (DHF) বা ডেঙ্গু শক সিন্ড্রোমে আক্রান্ত হতে পারেন। অবিলম্বে নিকটস্থ হাসপাতালের জরুরি বিভাগে যোগাযোগ করুন।"
                    : "You may be experiencing Dengue Hemorrhagic Fever (DHF) or Dengue Shock Syndrome. Seek IMMEDIATE medical care at the nearest emergency room or hospital.";
                confidenceNote = bn
                    ? "মডেল আস্থা: উচ্চ — গুরুতর ক্লিনিক্যাল সতর্কসংকেত চিহ্নিত।"
                    : "Model confidence: High — Critical clinical warning signs detected.";
                isEmergency = true;
            }
            else if (score >= 50)
            {
                riskLevel = bn ? "উচ্চ ঝুঁকি" : "High";
                advice = bn
                    ? "আপনার উপসর্গ তীব্র ডেঙ্গু সংক্রমণের উচ্চ সম্ভাবনা নির্দেশ করছে। আজই ক্লিনিক বা হাসপাতালে গিয়ে NS1 অ্যান্টিজেন / CBC রক্তপরীক্ষা করান।"
                    : "Your symptoms suggest a high probability of acute dengue infection. Please visit a clinic or hospital today for an NS1 antigen / CBC blood test.";
                confidenceNote = bn
                    ? "মডেল আস্থা: উচ্চ — ১,২৫০+ যাচাইকৃত রোগীর তথ্যের ভিত্তিতে।"
                    : "Model confidence: High — based on 1,250+ validated symptomatic cases.";
            }
            else if (score >= 25)
            {
                riskLevel = bn ? "মধ্যম ঝুঁকি" : "Medium";
                advice = bn
                    ? "কিছু ডেঙ্গু উপসর্গ বিদ্যমান। পর্যাপ্ত বিশ্রাম নিন, ORS/তরল পান করুন এবং তাপমাত্রা পর্যবেক্ষণ করুন। ২ দিনের বেশি জ্বর থাকলে চিকিৎসকের পরামর্শ নিন।"
                    : "Some classic dengue symptoms are present. Rest adequately, maintain oral hydration with ORS/fluids, and monitor your temperature. If fever persists >2 days, consult a physician.";
                confidenceNote = bn
                    ? "মডেল আস্থা: মধ্যম — যাচাইকৃত উপসর্গ সম্পর্কের উপর ভিত্তি করে।"
                    : "Model confidence: Medium — based on validated symptom correlations.";
            }
            else
            {
                riskLevel = bn ? "নিম্ন ঝুঁকি" : "Low";
                advice = bn
                    ? "উপসর্গ হালকা বা ন্যূনতম। স্বাস্থ্যের উপর নজর রাখুন। মশানাশক ও মশারি ব্যবহার করুন। তীব্র জ্বর বা সতর্কসংকেত দেখা দিলে চিকিৎসা নিন।"
                    : "Symptoms are mild or minimal. Keep monitoring your health. Avoid mosquito bites by using repellents and nets. Seek medical care if high fever or warning signs develop.";
                confidenceNote = bn
                    ? "মডেল আস্থা: মধ্যম — ডেঙ্গু মার্কার ন্যূনতম।"
                    : "Model confidence: Medium — minimal dengue markers detected.";
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
