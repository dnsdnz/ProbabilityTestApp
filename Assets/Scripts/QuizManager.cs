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
    public TMP_InputField nameInputField; 
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
    private List<int> results = new List<int>(); // correct1-wrong0
    private DatabaseReference databaseReference;
    
    private string collectorName; // Kullanıcının adı

    void Start()
    {
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
        if (currentPanelIndex < questionPanels.Count && !isAnswered)
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
        if (!string.IsNullOrEmpty(nameInputField.text))
        {
            collectorName = nameInputField.text;
            introPanel.SetActive(false);
            ShowPanel(0); 
        }
        else
        {
            Debug.LogWarning("Enter name.");
        }
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
            return;
        }
        
        questionPanels[index].panel.SetActive(true);
        currentPanelIndex = index;

        foreach (var button in questionPanels[index].allButtons)
        {
            button.onClick.RemoveAllListeners(); 
            button.onClick.AddListener(() => OnButtonClicked(button));
        }

        questionTimer = 5f;
        isAnswered = false;
    }
    
    void OnButtonClicked(Button clickedButton)
    {
        bool isCorrect = clickedButton == questionPanels[currentPanelIndex].correctButton;
        RegisterAnswer(isCorrect);
    }

    void RegisterAnswer(bool isCorrect)
    {
        isAnswered = true;
        results.Add(isCorrect ? 1 : 0); 
        ShowPanel(currentPanelIndex + 1); 
    }
    
    void FinishQuiz()
    {
        Debug.Log("Quiz done! sending to Firebase...");

        var currentDate = DateTime.Now.ToString("yyyy-MM-dd");
        string currentTime = DateTime.Now.ToString("HH:mm:ss");
                
        WriteData( collectorName,currentDate,"DenizC", "Kız", "Lise", "İlkokul",
            1, "İST", "evet", "2 yıl",currentTime,
            currentTime, 10,results[0],results[1],1);
    }
    
    void WriteData(string collectorName, string currentDate, string childName, string gender, string motherEducation, string fatherEducation,
        int siblingCount, string location, string getEducationBefore, string getEducationBeforeTime, string appStartTime, string appEndTime,
        int completeScore, int q1Score, int q2Score, int chapter1Score)
    {
        var userData = new System.Collections.Generic.Dictionary<string, object>
        {
            {"VeriToplayanKişi", collectorName},
            {"UygulamaTarihi", currentDate},
            {"ÇocuğunAdıSoyadı", childName},
            {"ÇocuğunCinsiyeti", gender},
            {"AnneÖğrenimDurumu", motherEducation},
            {"BabaÖğrenimDurumu", fatherEducation},
            {"KardeşSayısı", siblingCount},
            {"ÇocuğunYaşadığıİlİlçeKöy", location},
            {"DahaÖnceOkulÖncesiEğitimiAldımı", getEducationBefore},
            {"DahaÖnceOkulÖncesiEğitimiAlmaSüresi", getEducationBeforeTime},
            {"UygulamaBaşlamaZamanı", appStartTime},
            {"UygulamaBitişZamanı", appEndTime},
            {"TESTİNTAMAMINDANALINANTOPLAMPUAN", completeScore},
            {"1SorununPuanı", q1Score},
            {"2SorununPuanı", q2Score},
            {"1BÖLÜMTOPLAMPUANI", chapter1Score},
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