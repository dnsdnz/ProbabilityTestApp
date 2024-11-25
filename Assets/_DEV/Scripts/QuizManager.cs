using System;
using System.Collections;
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
    public GameObject pausePanel;

    public TMP_InputField collectorNameInputField;
    public TMP_InputField childNameInputField;
    public TMP_InputField birthdayInputField;

    public TMP_Dropdown genderDropdown;
    public TMP_Dropdown motherEducationDropdown;
    public TMP_Dropdown fatherEducationDropdown;
    public TMP_Dropdown siblingCountDropdown;
    public TMP_Dropdown locationDropdown;
    public TMP_Dropdown locationDetailDropdown;
    public TMP_Dropdown getPreEducationDropdown;
    public TMP_Dropdown preEducationTimeDropdown;
    
    public GameObject preEducationTimeDropdownParent;
    public Button startButton;

    public bool isPaused;

    [Serializable]
    public class QuestionPanel
    {
        public GameObject panel;
        public Button correctButton;
        public Button[] allButtons;
        public AudioClip questionAudio;
    }

    public List<QuestionPanel> questionPanels;
    public int currentPanelIndex = 0;
    public float questionTimer = 90f;
    public bool isAnswered = false;
    public bool isQuestionActive = false;

    public List<int> results = new List<int>(); // correct1-wrong0
    public List<string> questionOpenTimes = new List<string>();
    public List<string> questionAnswerTimes = new List<string>();
    public List<double> responseTimes = new List<double>();

    private DatabaseReference databaseReference;

    public AudioSource audioSource;

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
    private string locationDetail;
    private string getEducationBefore;
    private string preEducationTime;
    private string appStartTime;
    private string appEndTime;
    private double fullSessionTime;
    private int completeScore;

    public DateTime appStartDateTime;

    public float pauseStartTime;
    public float totalPauseTime;
    public float questionStartTime;

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
        if (isPaused)
            return;

        if (isQuestionActive && !isAnswered)
        {
            questionTimer -= Time.deltaTime;
            if (questionTimer <= 0)
            {
                questionTimer = 0;
                RegisterAnswer(false);
                ShowPanel(currentPanelIndex + 1);
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        pauseStartTime = Time.time;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        totalPauseTime += Time.time - pauseStartTime;
    }

    void OnStartButtonClicked()
    {
        //TODO OPEN CODES!!
        if (string.IsNullOrEmpty(collectorNameInputField.text) || string.IsNullOrEmpty(childNameInputField.text) ||
            string.IsNullOrEmpty(birthdayInputField.text) || genderDropdown.value == 0 ||
            motherEducationDropdown.value == 0 || fatherEducationDropdown.value == 0 ||
            siblingCountDropdown.value == 0 || locationDropdown.value == 0
            || locationDetailDropdown.value == 0 || getPreEducationDropdown.value == 0)
        {
            Debug.Log("Eksik bilgileri doldur!");
            return;
        }

        if (getPreEducationDropdown.value == 1)
        {
            if (preEducationTimeDropdown.value == 0)
            {
                Debug.Log("Eksik bilgileri doldur!");
                return;
            }
        }

        collectorName = collectorNameInputField.text;
        childName = childNameInputField.text;
        birthday = birthdayInputField.text;

        gender = genderDropdown.options[genderDropdown.value].text;
        motherEducation = motherEducationDropdown.options[motherEducationDropdown.value].text;
        fatherEducation = fatherEducationDropdown.options[fatherEducationDropdown.value].text;
        siblingCount = siblingCountDropdown.options[siblingCountDropdown.value].text;
        location = locationDropdown.options[locationDetailDropdown.value].text;
        locationDetail = locationDetailDropdown.options[locationDetailDropdown.value].text;
        getEducationBefore = getPreEducationDropdown.options[getPreEducationDropdown.value].text;
        preEducationTime = preEducationTimeDropdown.options[preEducationTimeDropdown.value].text;
        
        introPanel.SetActive(false);
        ShowPanel(0);
    }

    public void OpenGetEducationTimeInputField()
    {
        preEducationTimeDropdownParent.SetActive(getPreEducationDropdown.value == 1);
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
            //button.gameObject.SetActive(false);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnButtonClicked(button));
        }

        questionTimer = 90f;
        questionStartTime = Time.time;
        totalPauseTime = 0f;

        isAnswered = false;
        isQuestionActive = true;

        if (questionPanels[index].questionAudio != null)
        {
            audioSource.clip = questionPanels[index].questionAudio;
            audioSource.Play();

            //StartCoroutine(EnableButtonsAfterAudio(questionPanels[index].allButtons, audioSource.clip.length));
        }
    }

    IEnumerator EnableButtonsAfterAudio(Button[] buttons, float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (var button in buttons)
        {
            button.gameObject.SetActive(true);
        }
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

        float responseTime = Time.time - questionStartTime - totalPauseTime;
        Debug.Log($"Cevap süresi: {responseTime} saniye");

        results.Add(isCorrect ? 1 : 0);
        completeScore += isCorrect ? 1 : 0;

        questionAnswerTimes.Add(DateTime.Now.ToString("HH:mm:ss"));
        responseTimes.Add(responseTime);

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

        // int count = Mathf.Min(questionOpenTimes.Count, questionAnswerTimes.Count);
        // for (int i = 0; i < count; i++)
        // {
        //     DateTime time1 = DateTime.Parse(questionOpenTimes[i]);
        //     DateTime time2 = DateTime.Parse(questionAnswerTimes[i]);
        //
        //     TimeSpan difference = time2 - time1;
        //
        //     responseTimes.Add(difference.TotalMilliseconds);
        // }

        WriteData(collectorName, appStartDay, childName, birthday, gender, motherEducation, fatherEducation,
            siblingCount, location, locationDetail, getEducationBefore, preEducationTime, appStartTime, appEndTime, fullSessionTime,
            completeScore,
            results[0], results[1],
            results[0] + results[1],
            responseTimes[0], responseTimes[1]);
    }

    void WriteData(string collectorName, string appStartDay, string childName, string birthday, string gender,
        string motherEducation, string fatherEducation,
        string siblingCount, string location, string locationDetail, string getEducationBefore, string preEducationTime, string appStartTime,
        string appEndTime, double fullSessionTime,
        int completeScore, int q1Score, int q2Score, int chapter1Score, double q1responseTime, double q2responseTime)
    {
        var userData = new Dictionary<string, object>
        {
            { "A1_VeriToplayanKişi", collectorName },
            { "A2_UygulamaTarihi", appStartDay },
            { "A3_ÇocuğunAdıSoyadı", childName },
            { "A4_ÇocuğunDoğumTarihi", birthday },
            { "A5_ÇocuğunCinsiyeti", gender },
            { "A6_AnneÖğrenimDurumu", motherEducation },
            { "A7_BabaÖğrenimDurumu", fatherEducation },
            { "A8_KardeşSayısı", siblingCount },
            { "A9_ÇocuğunYaşadığıİl", location },
            { "B1_ÇocuğunYaşadığıİlİlçeKöy", locationDetail },
            { "B2_DahaÖnceOkulÖncesiEğitimiAldımı", getEducationBefore },
            { "B3_DahaÖnceOkulÖncesiEğitimiAlmaSüresi", preEducationTime },
            { "C_UygulamaBaşlamaZamanı", appStartTime },
            { "C_UygulamaBitişZamanı", appEndTime },
            { "D_ToplamOturumSüresi", fullSessionTime },
            { "E_TESTİNTAMAMINDANALINANTOPLAMPUAN", completeScore },
            { "F_1SorununPuanı", q1Score },
            { "F_2SorununPuanı", q2Score },
            { "G_1BÖLÜMTOPLAMPUANI", chapter1Score },
            { "H_1SorununTepkiSüresi", q1responseTime },
            { "H_2SorununTepkiSüresi", q2responseTime },
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