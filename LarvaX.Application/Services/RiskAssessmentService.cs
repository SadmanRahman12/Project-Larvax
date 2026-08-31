using LarvaX.Core.Entities;

namespace LarvaX.Application.Services
{
    public interface IRiskAssessmentService
    {
        RiskLevel CalculateRiskLevel(int verifiedCaseCount, int activeHazardCount);
        double CalculateConfidenceScore(int sampleCount);
        DataSufficiency DetermineDataSufficiency(int sampleCount);
        string GeneratePublicAdvice(RiskLevel level, string language = "en");
        string GenerateAlertMessage(string region, RiskLevel level, string disease, string language = "en");
    }

    public class RiskAssessmentService : IRiskAssessmentService
    {
        public RiskLevel CalculateRiskLevel(int verifiedCaseCount, int activeHazardCount)
        {
            int score = (verifiedCaseCount * 3) + (activeHazardCount * 2);

            if (score >= 20 || verifiedCaseCount >= 5)
            {
                return RiskLevel.High;
            }
            else if (score >= 8 || verifiedCaseCount >= 2)
            {
                return RiskLevel.Medium;
            }
            else
            {
                return RiskLevel.Low;
            }
        }

        public double CalculateConfidenceScore(int sampleCount)
        {
            if (sampleCount <= 0) return 0.1;
            if (sampleCount >= 50) return 0.95;
            if (sampleCount >= 20) return 0.85;
            if (sampleCount >= 10) return 0.70;
            return 0.40;
        }

        public DataSufficiency DetermineDataSufficiency(int sampleCount)
        {
            return sampleCount >= 10 ? DataSufficiency.Sufficient : DataSufficiency.Insufficient;
        }

        public string GeneratePublicAdvice(RiskLevel level, string language = "en")
        {
            bool bn = language == "bn";

            return level switch
            {
                RiskLevel.High => bn
                    ? "সতর্কতা: আপনার এলাকায় এডিস মশার তীব্র প্রজনন চিহ্নিত হয়েছে। প্রতিদিন জমা পানি পরিষ্কার করুন এবং অবিলম্বে জ্বর হলে রক্ত পরীক্ষা করান।"
                    : "HIGH ALERT: Elevated mosquito breeding and active clusters reported. Inspect all standing water containers daily and seek testing if fever occurs.",

                RiskLevel.Medium => bn
                    ? "মধ্যম ঝুঁকি: এলাকায় সম্ভাব্য মশার প্রজনন উৎস রয়েছে। সপ্তাহে অন্তত দুবার বাড়ির আশপাশ পরিষ্কার রাখুন।"
                    : "MODERATE RISK: Potential vector breeding detected. Check and empty rooftop containers and planters twice weekly.",

                _ => bn
                    ? "নিম্ন ঝুঁকি: এলাকাটি অপেক্ষাকৃত নিরাপদ। নিয়মিত পরিষ্কার-পরিচ্ছন্নতা বজায় রাখুন এবং মশারি ব্যবহার করুন।"
                    : "LOW RISK: Stable epidemiological status. Continue routine preventive hygiene and use mosquito netting."
            };
        }

        public string GenerateAlertMessage(string region, RiskLevel level, string disease, string language = "en")
        {
            bool bn = language == "bn";
            string levelText = level == RiskLevel.High ? (bn ? "উচ্চ ঝুঁকি" : "HIGH RISK") : (bn ? "সতর্কতা" : "ALERT");

            if (bn)
            {
                return $"[জরুরি সতর্কবার্তা] {region} অঞ্চলে {disease} এর {levelText} স্তর চিহ্নিত হয়েছে। অবিলম্বে প্রতিরোধমূলক পদক্ষেপ গ্রহণ করুন!";
            }
            return $"[SURVEILLANCE ALERT] {region} is now designated as {levelText} for {disease}. Please take immediate protective precautions!";
        }
    }
}
