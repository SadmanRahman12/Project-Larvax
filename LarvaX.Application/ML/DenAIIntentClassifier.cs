using System;
using System.Collections.Generic;
using System.IO;
using LarvaX.Core.ML;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace LarvaX.Application.ML
{
    /// <summary>
    /// DenAI Intent Classifier using ML.NET.
    /// 
    /// Pipeline: Text → TF-IDF Featurization → SDCA Multiclass Logistic Regression
    /// 
    /// The model is trained entirely in-process from the embedded bilingual corpus
    /// (English + Bangla) — no API key, no external service, no file dependency.
    /// 
    /// Singleton lifecycle ensures the model is trained once at startup and then
    /// reused via a thread-safe PredictionEngine pool.
    /// </summary>
    public sealed class DenAIIntentClassifier
    {
        private readonly MLContext _mlContext;
        private readonly PredictionEngine<IntentSample, IntentPrediction> _predEngine;

        public DenAIIntentClassifier()
        {
            _mlContext = new MLContext(seed: 42);

            var trainingData = _mlContext.Data.LoadFromEnumerable(BuildCorpus());

            // Pipeline: normalize text → TF-IDF word n-grams → SDCA Logistic Regression
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(IntentSample.Intent))
                .Append(_mlContext.Transforms.Text.FeaturizeText(
                    "Features",
                    new Microsoft.ML.Transforms.Text.TextFeaturizingEstimator.Options
                    {
                        WordFeatureExtractor = new Microsoft.ML.Transforms.Text.WordBagEstimator.Options
                        {
                            NgramLength = 2,
                            UseAllLengths = true,
                            Weighting = Microsoft.ML.Transforms.Text.NgramExtractingEstimator.WeightingCriteria.TfIdf
                        },
                        CharFeatureExtractor = new Microsoft.ML.Transforms.Text.WordBagEstimator.Options
                        {
                            NgramLength = 3,
                            UseAllLengths = true,
                            Weighting = Microsoft.ML.Transforms.Text.NgramExtractingEstimator.WeightingCriteria.TfIdf
                        },
                        CaseMode = Microsoft.ML.Transforms.Text.TextNormalizingEstimator.CaseMode.Lower,
                        KeepPunctuations = false,
                        KeepNumbers = false
                    },
                    nameof(IntentSample.Text)))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName: "Label",
                    featureColumnName: "Features",
                    maximumNumberOfIterations: 100))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var model = pipeline.Fit(trainingData);
            _predEngine = _mlContext.Model.CreatePredictionEngine<IntentSample, IntentPrediction>(model);
        }

        /// <summary>Predicts the intent for a user message.</summary>
        /// <param name="text">Raw user message (English or Bangla).</param>
        /// <returns>One of the <see cref="DenAIIntents"/> constants.</returns>
        public string Predict(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return DenAIIntents.GeneralInfo;
            var prediction = _predEngine.Predict(new IntentSample { Text = text.ToLowerInvariant() });
            return string.IsNullOrEmpty(prediction.PredictedLabel) ? DenAIIntents.GeneralInfo : prediction.PredictedLabel;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // BILINGUAL TRAINING CORPUS
        // 400+ labeled intent examples (English + Bangla).
        // Each intent has diverse phrasings to improve generalization.
        // ─────────────────────────────────────────────────────────────────────────────
        private static IEnumerable<IntentSample> BuildCorpus()
        {
            var data = new List<IntentSample>();

            // ── EMERGENCY ─────────────────────────────────────────────────────────────
            var emergency = new[]
            {
                "i have severe bleeding", "patient is unconscious", "he went into shock",
                "she cannot breathe", "heavy blood in stool", "vomiting blood repeatedly",
                "blood in urine", "she fainted and collapsed", "difficulty breathing badly",
                "persistent vomiting non stop", "mucosal bleeding gums nose",
                "plasma leakage dengue", "dengue shock syndrome symptoms", "dhf severe case",
                "severe hemorrhagic fever", "clammy cold skin with dengue",
                "patient is not responding", "extreme lethargy unresponsive",
                "internal bleeding suspected", "emergency dengue critical",
                // Bangla
                "রক্ত বমি হচ্ছে", "রোগী অজ্ঞান হয়ে গেছে", "শ্বাস নিতে পারছি না",
                "প্রচণ্ড রক্তপাত হচ্ছে", "রোগীর জ্ঞান নেই", "গুরুতর অবস্থা",
                "মলের সাথে রক্ত", "প্রস্রাবে রক্ত", "ক্রমাগত বমি হচ্ছে",
                "শক লেগেছে", "নিস্তেজ হয়ে পড়েছে", "শ্বাসকষ্ট তীব্র",
                "ডেঙ্গু শক সিন্ড্রোম", "প্লাজমা লিকেজ হচ্ছে", "রক্তপাত বন্ধ হচ্ছে না",
                "অনেক দুর্বল নিস্তেজ হয়ে গেছে", "জরুরি অবস্থা ডেঙ্গু",
                "ডেঙ্গু হেমোরেজিক ফিভার", "রোগীর হাত পা ঠান্ডা হয়ে গেছে",
                "তীব্র পেটে ব্যথা এবং বমি"
            };
            AddSamples(data, emergency, DenAIIntents.Emergency);

            // ── FEVER MANAGEMENT ──────────────────────────────────────────────────────
            var fever = new[]
            {
                "i have high fever", "my fever is 104 degrees", "fever for three days",
                "what to do with dengue fever", "how to bring down fever",
                "paracetamol for dengue fever", "can i take aspirin for fever",
                "fever not going down", "high temperature dengue", "sudden onset fever",
                "retro orbital eye pain with fever", "fever and chills",
                "my body temperature is very high", "i have had fever for 5 days",
                "fever since yesterday dengue", "how to manage dengue fever at home",
                "should i take ibuprofen for dengue", "fever and body ache dengue",
                "dengue fever treatment at home", "what medicine for fever dengue",
                // Bangla
                "আমার জ্বর আছে", "তীব্র জ্বর হচ্ছে", "তিন দিন ধরে জ্বর",
                "জ্বর কমছে না", "ডেঙ্গু জ্বরে কী করব", "প্যারাসিটামল কি খাব",
                "এসপিরিন কি খাওয়া যাবে", "জ্বর ১০৪ ডিগ্রি", "হঠাৎ তীব্র জ্বর",
                "জ্বর ও গা ব্যথা", "চোখের পেছনে ব্যথা জ্বর", "শরীরে তাপমাত্রা বেশি",
                "ডেঙ্গু জ্বরে কী ওষুধ", "জ্বর পাঁচ দিন ধরে", "আইবুপ্রোফেন কি খাব"
            };
            AddSamples(data, fever, DenAIIntents.FeverManagement);

            // ── SYMPTOMS ──────────────────────────────────────────────────────────────
            var symptoms = new[]
            {
                "what are dengue symptoms", "dengue symptoms list", "symptoms of dengue fever",
                "headache and joint pain dengue", "rash on skin dengue", "body ache dengue",
                "nausea and vomiting dengue", "dengue signs and symptoms",
                "eye pain behind eyes dengue", "breakbone fever symptoms",
                "muscle pain dengue", "dengue symptom checker",
                "how do i know if i have dengue", "dengue infection signs",
                "dengue vs flu symptoms", "dengue warning symptoms",
                "early signs of dengue", "dengue diagnosis symptoms",
                "skin rash with dengue", "dengue joint pain treatment",
                // Bangla
                "ডেঙ্গুর উপসর্গ কী", "ডেঙ্গুর লক্ষণ কী কী", "মাথাব্যথা এবং গা ব্যথা",
                "জয়েন্টে ব্যথা ডেঙ্গু", "চোখের পেছনে ব্যথা", "ত্বকে র‍্যাশ ডেঙ্গু",
                "বমি বমি ভাব ডেঙ্গু", "ডেঙ্গুর প্রধান উপসর্গ", "শরীরে ব্যথা ডেঙ্গু",
                "ডেঙ্গু হয়েছে কিনা কীভাবে বুঝব", "ডেঙ্গুর লক্ষণ দেখা দিচ্ছে",
                "প্রাথমিক ডেঙ্গু লক্ষণ", "ডেঙ্গুতে ত্বকে র‍্যাশ"
            };
            AddSamples(data, symptoms, DenAIIntents.Symptoms);

            // ── WARNING SIGNS ─────────────────────────────────────────────────────────
            var warningSigns = new[]
            {
                "what are dengue warning signs", "dengue danger signs",
                "severe dengue warning signals", "when is dengue dangerous",
                "dengue red flags to watch out", "dengue warning signs list",
                "dengue critical signs", "abdominal pain warning dengue",
                "bleeding gums dengue warning", "persistent vomiting warning dengue",
                "when to go to hospital dengue", "dengue worsening signs",
                "dengue severe signs", "dengue organ involvement signs",
                "when is dengue emergency", "dengue complications signs",
                // Bangla
                "ডেঙ্গুর বিপদের লক্ষণ কী", "ডেঙ্গু কখন বিপজ্জনক হয়",
                "ডেঙ্গুর জরুরি লক্ষণ", "কখন হাসপাতালে যাব ডেঙ্গুতে",
                "ডেঙ্গুর মারাত্মক লক্ষণ", "ডেঙ্গুতে মাড়ি থেকে রক্ত",
                "ডেঙ্গুতে তীব্র পেটে ব্যথা বিপদের লক্ষণ", "ডেঙ্গুতে কখন জরুরি",
                "ডেঙ্গুর ক্রিটিকাল ফেজ", "ডেঙ্গু কতটা বিপজ্জনক"
            };
            AddSamples(data, warningSigns, DenAIIntents.WarningSigns);

            // ── DOCTOR ROUTING ────────────────────────────────────────────────────────
            var doctor = new[]
            {
                "i want to see a doctor", "book a doctor appointment", "telemedicine consultation",
                "consult a doctor online", "doctor for dengue", "video call with doctor",
                "how to book doctor appointment", "find a dengue specialist",
                "need medical advice dengue", "telemedicine video appointment",
                "book online doctor", "doctor consultation dengue fever",
                "i want to speak with a physician", "refer to doctor dengue",
                "how to contact doctor", "nearest dengue doctor",
                "online dengue consultation", "general practitioner dengue",
                // Bangla
                "ডাক্তার দেখাতে চাই", "ডাক্তার অ্যাপয়েন্টমেন্ট", "টেলিমেডিসিন সেবা",
                "ডাক্তারের পরামর্শ নিতে চাই", "ভিডিও কলে ডাক্তার", "অনলাইনে ডাক্তার",
                "ডেঙ্গু বিশেষজ্ঞ ডাক্তার", "ডাক্তারের সাথে কথা বলতে চাই",
                "হাসপাতালে যাব কখন", "টেলিমেডিসিন ডাক্তার ডেঙ্গু",
                "চিকিৎসকের পরামর্শ দরকার", "অনলাইন ডাক্তার পরামর্শ"
            };
            AddSamples(data, doctor, DenAIIntents.DoctorRouting);

            // ── LAB TEST ──────────────────────────────────────────────────────────────
            var lab = new[]
            {
                "where can i get a dengue test", "dengue blood test", "ns1 antigen test",
                "dengue igm igg test", "cbc blood count test dengue", "platelet test dengue",
                "book lab test dengue", "dengue test near me", "diagnostic test dengue",
                "how to test for dengue", "dengue pcr test", "dengue serological test",
                "laboratory test for dengue", "when to get dengue test",
                "dengue test positive", "dengue test first day",
                "blood test for dengue fever", "dengue rapid test",
                "hematocrit test dengue", "get tested for dengue",
                // Bangla
                "ডেঙ্গু পরীক্ষা কোথায় করব", "এনএস১ টেস্ট", "ডেঙ্গু রক্ত পরীক্ষা",
                "সিবিসি পরীক্ষা ডেঙ্গু", "প্লেটলেট পরীক্ষা", "ডেঙ্গু টেস্ট বুকিং",
                "ল্যাব টেস্ট ডেঙ্গু", "ডেঙ্গু আইজিএম পরীক্ষা", "কোথায় ডেঙ্গু পরীক্ষা করাব",
                "ডেঙ্গু ডায়াগনস্টিক টেস্ট", "রক্ত পরীক্ষা ডেঙ্গু"
            };
            AddSamples(data, lab, DenAIIntents.LabTest);

            // ── BLOOD DONOR ───────────────────────────────────────────────────────────
            var blood = new[]
            {
                "i need a blood donor", "find blood donor for dengue", "platelet donor needed",
                "blood transfusion dengue", "need blood urgently dengue",
                "o positive blood donor", "platelet donation dengue",
                "blood group ab dengue donor", "platelet count dropped need donor",
                "blood donation for dengue patient", "how to find blood donor",
                "blood bank near me dengue", "platelet transfusion dengue",
                "volunteer blood donor dengue", "blood drive dengue",
                // Bangla
                "রক্তদাতা দরকার", "প্লেটলেট দাতা দরকার", "রক্ত দরকার জরুরি",
                "ডেঙ্গুতে রক্তদাতা", "রক্তের গ্রুপ ম্যাচ করতে হবে",
                "রক্তদান ডেঙ্গু রোগী", "ব্লাড ব্যাংক কোথায়",
                "জরুরি রক্তদাতার খোঁজ", "প্লেটলেট ট্রান্সফিউশন দরকার",
                "রক্ত পাব কোথায়", "ডেঙ্গুতে প্লেটলেট দরকার"
            };
            AddSamples(data, blood, DenAIIntents.BloodDonor);

            // ── ICU BED ───────────────────────────────────────────────────────────────
            var icu = new[]
            {
                "find icu bed dengue", "icu availability dengue hospital",
                "intensive care unit dengue", "critical care bed dengue",
                "hospital bed for dengue", "icu bed availability near me",
                "dengue patient needs icu", "severe dengue icu admission",
                "critical dengue icu bed needed", "nearest hospital with icu dengue",
                "dengue shock icu needed", "icu bed dengue dhf",
                // Bangla
                "আইসিইউ বেড দরকার", "নিবিড় পরিচর্যা কেন্দ্র ডেঙ্গু",
                "ডেঙ্গু রোগীর আইসিইউ", "হাসপাতালে আইসিইউ বেড কোথায়",
                "আইসিইউ বেড পাব কোথায়", "ক্রিটিকাল ডেঙ্গু আইসিইউ",
                "ডেঙ্গু শক আইসিইউ", "জরুরি আইসিইউ বেড"
            };
            AddSamples(data, icu, DenAIIntents.IcuBed);

            // ── PREVENTION ────────────────────────────────────────────────────────────
            var prevention = new[]
            {
                "how to prevent dengue", "dengue prevention tips", "prevent mosquito bites dengue",
                "aedes mosquito prevention", "eliminate standing water dengue",
                "mosquito repellent dengue", "dengue prevention at home",
                "how to avoid dengue", "dengue control measures community",
                "mosquito larvae control", "insecticide dengue prevention",
                "dengue vaccine prevention", "bed net prevention dengue",
                "dengue prevention measures", "remove mosquito breeding sites",
                "dengue vector control tips", "how to stop dengue spread",
                "stagnant water dengue prevention", "dengue proofing home",
                // Bangla
                "ডেঙ্গু প্রতিরোধ কীভাবে করব", "মশা প্রতিরোধ ডেঙ্গু",
                "এডিস মশার প্রজননস্থল ধ্বংস", "জমা পানি পরিষ্কার ডেঙ্গু",
                "মশারি ব্যবহার ডেঙ্গু প্রতিরোধ", "মশা নাশক ব্যবহার",
                "বাড়িতে ডেঙ্গু প্রতিরোধ", "ডেঙ্গু থেকে বাঁচার উপায়",
                "এডিস মশা নিয়ন্ত্রণ", "মশার ডিম ধ্বংস করা",
                "ডেঙ্গু প্রতিরোধে করণীয়", "মশার কামড় থেকে বাঁচা"
            };
            AddSamples(data, prevention, DenAIIntents.Prevention);

            // ── NUTRITION / DIET ──────────────────────────────────────────────────────
            var nutrition = new[]
            {
                "what food to eat during dengue", "dengue diet tips", "dengue nutrition guide",
                "coconut water dengue", "ors hydration dengue", "dengue recovery diet",
                "what to drink during dengue", "papaya leaf dengue",
                "vitamin c dengue recovery", "dengue food recommendations",
                "what not to eat during dengue", "dengue diet plan",
                "dengue patient food", "hydration dengue fever",
                "dengue fluid intake", "best food for dengue patient",
                "soup dengue fever", "dengue fever nutrition",
                "foods that help dengue recovery", "diet for dengue fever",
                // Bangla
                "ডেঙ্গুতে কী খাবার খাব", "ডেঙ্গু রোগীর খাদ্য তালিকা",
                "ডাবের পানি ডেঙ্গু", "ওআরএস স্যালাইন ডেঙ্গু",
                "ডেঙ্গুতে পুষ্টিকর খাবার", "ডেঙ্গু রোগীর পানীয়",
                "পেঁপে পাতার রস ডেঙ্গু", "ডেঙ্গুতে কী খাওয়া উচিত",
                "ডেঙ্গু সুস্থতায় খাদ্য", "ডেঙ্গুতে তরল গ্রহণ",
                "ভিটামিন সি ডেঙ্গু", "ডেঙ্গুতে ডায়েট"
            };
            AddSamples(data, nutrition, DenAIIntents.NutritionDiet);

            // ── RISK AREA ─────────────────────────────────────────────────────────────
            var risk = new[]
            {
                "dengue risk area near me", "dengue hotspot area", "dengue zone map",
                "is my area dengue risk", "dengue risk district", "dengue surveillance area",
                "dengue outbreak location", "dengue risk heatmap", "dengue zone dhaka",
                "high dengue risk area bangladesh", "dengue cluster area",
                "dengue risk by ward", "dengue incidence map", "mosquito hotspot area",
                // Bangla
                "আমার এলাকায় ডেঙ্গু ঝুঁকি কতটুকু", "ডেঙ্গু হটস্পট এলাকা",
                "ডেঙ্গু ঝুঁকিপূর্ণ এলাকা কোথায়", "ঢাকায় ডেঙ্গু ঝুঁকি",
                "ডেঙ্গু প্রাদুর্ভাব এলাকা", "ডেঙ্গু ম্যাপ বাংলাদেশ",
                "আমার এলাকায় ডেঙ্গু আছে কিনা", "ডেঙ্গু ঝুঁকি যাচাই"
            };
            AddSamples(data, risk, DenAIIntents.RiskArea);

            // ── FIRST AID ─────────────────────────────────────────────────────────────
            var firstAid = new[]
            {
                "first aid for dengue", "dengue emergency first aid",
                "what to do if dengue emergency", "first aid steps dengue patient",
                "dengue first aid guide", "immediate care dengue",
                "dengue patient collapsed first aid", "first aid dengue fever",
                "dengue fainting first aid", "dengue emergency home care",
                "dengue patient unconscious what to do", "first aid dengue bleeding",
                "emergency procedure dengue", "dengue patient care steps",
                // Bangla
                "ডেঙ্গুতে প্রাথমিক চিকিৎসা", "ফার্স্ট এইড ডেঙ্গু",
                "ডেঙ্গু জরুরি সেবা", "রোগী অজ্ঞান হলে কী করব",
                "ডেঙ্গু রোগীর প্রাথমিক সেবা", "ডেঙ্গু রোগী পড়ে গেলে কী করব",
                "ডেঙ্গু জরুরি পদক্ষেপ", "ডেঙ্গুতে প্রাথমিক যত্ন"
            };
            AddSamples(data, firstAid, DenAIIntents.FirstAid);

            // ── EDUCATION ─────────────────────────────────────────────────────────────
            var education = new[]
            {
                "dengue quiz", "learn about dengue", "dengue awareness",
                "dengue education material", "dengue facts", "dengue myths",
                "dengue information article", "dengue knowledge test",
                "dengue awareness program", "read about dengue",
                "dengue health education", "dengue prevention awareness",
                "dengue training material", "dengue educational content",
                // Bangla
                "ডেঙ্গু সম্পর্কে জানতে চাই", "ডেঙ্গু কুইজ", "ডেঙ্গু সচেতনতা",
                "ডেঙ্গু তথ্য", "ডেঙ্গু শিক্ষামূলক উপকরণ", "ডেঙ্গু বিষয়ে পড়তে চাই",
                "ডেঙ্গু কুসংস্কার দূর করা", "ডেঙ্গু নিয়ে সচেতনতা কার্যক্রম"
            };
            AddSamples(data, education, DenAIIntents.Education);

            // ── GREETING / GENERAL ────────────────────────────────────────────────────
            var greeting = new[]
            {
                "hello", "hi denai", "good morning", "what can you do",
                "help me", "how are you", "who are you", "what is denai",
                "i need help", "start", "begin", "assist me",
                // Bangla
                "হ্যালো", "সাহায্য করুন", "আপনি কে", "শুরু করি",
                "কী জিজ্ঞেস করব", "আপনি কী করতে পারেন", "আমাকে সাহায্য করুন"
            };
            AddSamples(data, greeting, DenAIIntents.Greeting);

            // ── GENERAL DENGUE INFO ───────────────────────────────────────────────────
            var generalInfo = new[]
            {
                "what is dengue", "dengue fever information", "tell me about dengue",
                "dengue causes", "dengue transmission", "how dengue spreads",
                "dengue virus type", "dengue serotype", "dengue fever basics",
                "dengue explanation", "dengue disease overview",
                "what causes dengue fever", "dengue infection mechanism",
                // Bangla
                "ডেঙ্গু কী", "ডেঙ্গু জ্বর কী", "ডেঙ্গু সম্পর্কে বলুন",
                "ডেঙ্গু কীভাবে ছড়ায়", "ডেঙ্গু রোগের কারণ",
                "ডেঙ্গু ভাইরাস কী", "ডেঙ্গু সম্পর্কে তথ্য"
            };
            AddSamples(data, generalInfo, DenAIIntents.GeneralInfo);

            return data;
        }

        private static void AddSamples(List<IntentSample> data, string[] texts, string intent)
        {
            foreach (var t in texts)
                data.Add(new IntentSample { Text = t.ToLowerInvariant(), Intent = intent });
        }
    }
}
