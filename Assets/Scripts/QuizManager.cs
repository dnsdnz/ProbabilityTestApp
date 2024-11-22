using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    public GameObject introPanel;
    public GameObject successPanel;

    public TMP_InputField collectorNameInputField; 
    public TMP_InputField childNameInputField;
    public TMP_InputField birthdayInputField;
    public TMP_InputField locationInputField;
    public TMP_InputField preEducationTimeInputField;
    
    public TMP_Dropdown genderDropdown;
    public TMP_Dropdown motherEducationDropdown;
    public TMP_Dropdown fatherEducationDropdown;
    public TMP_Dropdown siblingCountDropdown;
    public TMP_Dropdown getPreEducationDropdown;

    public Button startButton;
    
    [Serializable]
    public class QuestionPanel
    {
        public GameObject panel;
        public Button correctButton;
        public Button[] allButtons;
    }

    public List<QuestionPanel> questionPanels; 
    private int currentPanelIndex = 0;
    private float questionTimer = 5f; //TODO 90 sn yap
    private bool isAnswered = false;
    private bool isQuestionActive = false;
    private List<int> results = new List<int>(); // correct1-wrong0
    
    private List<string> questionOpenTimes = new List<string>();
    private List<string> questionAnswerTimes = new List<string>();
    private List<double> responseTimes = new List<double>(); 

    private DatabaseReference databaseReference;
    
    //Veri toplayan kişi----
    //Uygulama tarihi----
    //Çocuğun adı soyadı---
    //Çocuğun Doğum Tarihi----
    //Çocuğun cinsiyeti---
    //Anne öğrenim durumu---
    //Baba öğrenim durumu----
    //Kardeş sayısı---
    //Çocuğun yaşadığı il/ilçe/köy---
    //Daha önce okul öncesi eğitimi aldı mı---
    //Daha önce okul öncesi eğitimi alma süresi---
    //Uygulama başlama zamanı---
    //Uygulama bitiş zamanı---
    //Toplam oturum süresi---
    //TESTİN TAMAMINDAN ALINAN TOPLAM PUAN
    //1... sorunun puanı
    //1... BÖLÜM TOPLAM PUANI
    
    private string collectorName;
    private string appStartDay;
    private string childName;
    private string birthday;
    private string gender; 
    private string motherEducation; 
    private string fatherEducation; 
    private string siblingCount; 
    private string location; 
    private string getEducationBefore;
    private string preEducationTime;
    private string appStartTime;
    private string appEndTime;
    private double fullSessionTime;
    private int completeScore;
    
    private DateTime appStartDateTime;

    void Start()
    {
        completeScore = 0;
        
        appStartDay = DateTime.Now.ToString("yyyy-MM-dd");
        appStartTime = DateTime.Now.ToString("HH:mm:ss");
        appStartDateTime = DateTime.Now;
        
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase Connection Successful!");
            }
            else
            {
                Debug.LogError($"Firebase cant start: {task.Result}");
            }
        });

        introPanel.SetActive(true);
        
        foreach (var question in questionPanels)
        {
            question.panel.SetActive(false);
        }

        startButton.onClick.AddListener(OnStartButtonClicked);
    }
    
    void Update()
    {
        if (isQuestionActive && !isAnswered)
        {
            questionTimer -= Time.deltaTime;
            if (questionTimer <= 0)
            {
                RegisterAnswer(false);
            }
        }
    }
    
    void OnStartButtonClicked()
    {
        collectorName = collectorNameInputField.text;
        childName = childNameInputField.text;
        birthday = birthdayInputField.text;
        location = locationInputField.text;
        preEducationTime = preEducationTimeInputField.text;
        
        gender = genderDropdown.options[genderDropdown.value].text; 
        motherEducation = motherEducationDropdown.options[motherEducationDropdown.value].text; 
        fatherEducation = fatherEducationDropdown.options[fatherEducationDropdown.value].text;
        siblingCount = siblingCountDropdown.options[siblingCountDropdown.value].text;
        getEducationBefore = getPreEducationDropdown.options[getPreEducationDropdown.value].text;

        introPanel.SetActive(false);
        ShowPanel(0);
    }

    void ShowPanel(int index)
    {
        foreach (var panel in questionPanels)
        {
            panel.panel.SetActive(false);
        }
        
        if (index >= questionPanels.Count)
        {
            FinishQuiz();
            successPanel.SetActive(true);
            return;
        }
        
        questionPanels[index].panel.SetActive(true);
        currentPanelIndex = index;
        
        questionOpenTimes.Add(DateTime.Now.ToString("HH:mm:ss"));

        foreach (var button in questionPanels[index].allButtons)
        {
            button.onClick.RemoveAllListeners(); 
            button.onClick.AddListener(() => OnButtonClicked(button));
        }

        questionTimer = 5f;
        isAnswered = false;
        isQuestionActive = true;
    }
    
    void OnButtonClicked(Button clickedButton)
    {
        bool isCorrect = clickedButton == questionPanels[currentPanelIndex].correctButton;
        RegisterAnswer(isCorrect);
    }

    void RegisterAnswer(bool isCorrect)
    {
        isAnswered = true;
        isQuestionActive = false;
        results.Add(isCorrect ? 1 : 0); 
        completeScore += isCorrect ? 1 : 0;
        questionAnswerTimes.Add(DateTime.Now.ToString("HH:mm:ss"));
        ShowPanel(currentPanelIndex + 1); 
    }
    
    void FinishQuiz()
    {
        Debug.Log("Quiz done! sending to Firebase...");

        appEndTime = DateTime.Now.ToString("HH:mm:ss");
        fullSessionTime = (DateTime.Now - appStartDateTime).TotalSeconds;

        // Debug.Log(TimeSpan.FromSeconds(numOfSecs).Hours); 
        // Debug.Log(TimeSpan.FromSeconds(numOfSecs).Minutes);
        // Debug.Log(TimeSpan.FromSeconds(numOfSecs).Seconds); 

        int count = Mathf.Min(questionOpenTimes.Count, questionAnswerTimes.Count); 
        for (int i = 0; i < count; i++)
        {
            DateTime time1 = DateTime.Parse(questionOpenTimes[i]);
            DateTime time2 = DateTime.Parse(questionAnswerTimes[i]);

            TimeSpan difference = time2 - time1;

            responseTimes.Add(difference.TotalMilliseconds);
        }

        WriteData(collectorName, appStartDay, childName, birthday, gender, motherEducation, fatherEducation,
            siblingCount, location, getEducationBefore, preEducationTime, appStartTime, appEndTime, fullSessionTime,completeScore,
            results[0],results[1],
            results[0]+results[1],
            responseTimes[0], responseTimes[1]);
    }
    
    void WriteData(string collectorName, string appStartDay, string childName, string birthday, string gender, string motherEducation, string fatherEducation,
        string siblingCount, string location, string getEducationBefore, string preEducationTime, string appStartTime, string appEndTime, double fullSessionTime,
        int completeScore, int q1Score, int q2Score, int chapter1Score, double q1responseTime, double q2responseTime)
    {
        var userData = new Dictionary<string, object>
        {
            {"VeriToplayanKişi", collectorName},
            {"UygulamaTarihi", appStartDay},
            {"ÇocuğunAdıSoyadı", childName},
            {"ÇocuğunDoğumTarihi", birthday},
            {"ÇocuğunCinsiyeti", gender},
            {"AnneÖğrenimDurumu", motherEducation},
            {"BabaÖğrenimDurumu", fatherEducation},
            {"KardeşSayısı", siblingCount},
            {"ÇocuğunYaşadığıİlİlçeKöy", location},
            {"DahaÖnceOkulÖncesiEğitimiAldımı", getEducationBefore},
            {"DahaÖnceOkulÖncesiEğitimiAlmaSüresi", preEducationTime},
            {"UygulamaBaşlamaZamanı", appStartTime},
            {"UygulamaBitişZamanı", appEndTime},
            {"ToplamOturumSüresi", fullSessionTime},
            {"TESTİNTAMAMINDANALINANTOPLAMPUAN", completeScore},
            {"1SorununPuanı", q1Score},
            {"2SorununPuanı", q2Score},
            {"1BÖLÜMTOPLAMPUANI", chapter1Score},
            {"1SorununTepkiSüresi", q1responseTime},
            {"2SorununTepkiSüresi", q2responseTime},

        };

        databaseReference
            .Child("users")      
            .Push()     
            .SetValueAsync(userData)  
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"User added!");
                }
                else
                {
                    Debug.LogError("Data send error!");
                }
            });
    }
}