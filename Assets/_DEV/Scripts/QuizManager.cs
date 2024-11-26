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
    public AudioSource nextPageAudioSource;

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
        // if (string.IsNullOrEmpty(collectorNameInputField.text) || string.IsNullOrEmpty(childNameInputField.text) ||
        //     string.IsNullOrEmpty(birthdayInputField.text) || genderDropdown.value == 0 ||
        //     motherEducationDropdown.value == 0 || fatherEducationDropdown.value == 0 ||
        //     siblingCountDropdown.value == 0 || locationDropdown.value == 0
        //     || locationDetailDropdown.value == 0 || getPreEducationDropdown.value == 0)
        // {
        //     Debug.Log("Eksik bilgileri doldur!");
        //     return;
        // }
        //
        // if (getPreEducationDropdown.value == 1)
        // {
        //     if (preEducationTimeDropdown.value == 0)
        //     {
        //         Debug.Log("Eksik bilgileri doldur!");
        //         return;
        //     }
        // }

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
        
        nextPageAudioSource.Play();
        StartCoroutine(ShowPanelAfterDelay(currentPanelIndex + 1,1));
    }
    
    IEnumerator ShowPanelAfterDelay(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowPanel(index);
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
            responseTimes[0], responseTimes[1],responseTimes[0]+responseTimes[1]);
    }

    void WriteData(string collectorName, string appStartDay, string childName, string birthday, string gender,
        string motherEducation, string fatherEducation,
        string siblingCount, string location, string locationDetail, string getEducationBefore, string preEducationTime, string appStartTime,
        string appEndTime, double fullSessionTime,
        int completeScore, int q1Score, int q2Score, int chapter1Score, double q1responseTime, double q2responseTime, double chapter1responseTime)
    {
        var userData = new Dictionary<string, object>
        {
            { "A0_VeriToplayanKişi", collectorName },
            { "A1_UygulamaTarihi", appStartDay },
            { "A2_ÇocuğunAdıSoyadı", childName },
            { "A3_ÇocuğunDoğumTarihi", birthday },
            { "A4_ÇocuğunCinsiyeti", gender },
            { "A5_AnneÖğrenimDurumu", motherEducation },
            { "A6_BabaÖğrenimDurumu", fatherEducation },
            { "A7_KardeşSayısı", siblingCount },
            { "A8_ÇocuğunYaşadığıİl", location },
            { "A9_ÇocuğunYaşadığıİlİlçeKöy", locationDetail },
            { "B0_DahaÖnceOkulÖncesiEğitimiAldımı", getEducationBefore },
            { "B1_DahaÖnceOkulÖncesiEğitimiAlmaSüresi", preEducationTime },
            { "B2_UygulamaBaşlamaZamanı", appStartTime },
            { "B3_UygulamaBitişZamanı", appEndTime },
            { "B4_ToplamOturumSüresi", fullSessionTime },
            { "B5_TESTİNTAMAMINDANALINANTOPLAMPUAN", completeScore },
            { "B6_1SorununPuanı", q1Score },
            { "B7_2SorununPuanı", q2Score },
            { "B8_3SorununPuanı", q2Score },
            { "B9_4SorununPuanı", q2Score },
            { "C0_5SorununPuanı", q2Score },
            { "C1_6SorununPuanı", q2Score },
            { "C2_7SorununPuanı", q2Score },
            { "C3_8SorununPuanı", q2Score },
            { "C4_9SorununPuanı", q2Score },
            { "C5_10SorununPuanı", q2Score },
            { "C6_11SorununPuanı", q2Score },
            { "C7_12SorununPuanı", q2Score },
            { "C8_13SorununPuanı", q2Score },
            { "C9_14SorununPuanı", q2Score },
            { "D0_15SorununPuanı", q2Score },
            { "D1_16SorununPuanı", q2Score },
            { "D2_17SorununPuanı", q2Score },
            { "D3_18SorununPuanı", q2Score },
            { "D4_19SorununPuanı", q2Score },
            { "D5_20SorununPuanı", q2Score },
            { "D6_21SorununPuanı", q2Score },
            { "D7_22SorununPuanı", q2Score },
            { "D8_23SorununPuanı", q2Score },
            { "D9_24SorununPuanı", q2Score },
            { "E0_1BÖLÜMTOPLAMPUANI", chapter1Score },
            { "E1_1SorununTepkiSüresi", q1responseTime },
            { "E2_2SorununTepkiSüresi", q2responseTime },
            { "E3_3SorununTepkiSüresi", q1responseTime },
            { "E4_4SorununTepkiSüresi", q2responseTime },
            { "E5_5SorununTepkiSüresi", q1responseTime },
            { "E6_6SorununTepkiSüresi", q2responseTime },
            { "E7_7SorununTepkiSüresi", q1responseTime },
            { "E8_8SorununTepkiSüresi", q2responseTime },
            { "E9_9SorununTepkiSüresi", q1responseTime },
            { "F0_10SorununTepkiSüresi", q2responseTime },
            { "F1_11SorununTepkiSüresi", q1responseTime },
            { "F2_12SorununTepkiSüresi", q2responseTime },
            { "F3_13SorununTepkiSüresi", q1responseTime },
            { "F4_14SorununTepkiSüresi", q2responseTime },
            { "F5_15SorununTepkiSüresi", q1responseTime },
            { "F6_16SorununTepkiSüresi", q2responseTime },
            { "F7_17SorununTepkiSüresi", q1responseTime },
            { "F8_18SorununTepkiSüresi", q2responseTime },
            { "F9_19SorununTepkiSüresi", q2Score },
            { "G0_20SorununTepkiSüresi", q2Score },
            { "G1_21SorununTepkiSüresi", q2Score },
            { "G2_22SorununTepkiSüresi", q2Score },
            { "G3_23SorununTepkiSüresi", q2Score },
            { "G4_24SorununTepkiSüresi", q2Score },
            { "G5_1BÖLÜMTOPLAMTEPKİSÜRESİ", chapter1responseTime},
            { "G6_25SorununPuanı", q1responseTime },
            { "G7_26SorununPuanı", q2responseTime },
            { "G8_27SorununPuanı", q1responseTime },
            { "G9_28SorununPuanı", q2responseTime },
            { "H0_29SorununPuanı", q1responseTime },
            { "H1_30SorununPuanı", q2responseTime },
            { "H2_2BÖLÜMTOPLAMPUANI", chapter1Score },
            { "H3_25SorununTepkiSüresi", q1responseTime },
            { "H4_26SorununTepkiSüresi", q2responseTime },
            { "H5_27SorununTepkiSüresi", q1responseTime },
            { "H6_28SorununTepkiSüresi", q2responseTime },
            { "H7_29SorununTepkiSüresi", q1responseTime },
            { "H8_30SorununTepkiSüresi", q1responseTime },
            { "H9_2BÖLÜMTOPLAMTEPKİSÜRESİ", chapter1responseTime},
            { "I0_31SorununPuanı", q1responseTime },
            { "I1_32SorununPuanı", q2responseTime },
            { "I2_33SorununPuanı", q1responseTime },
            { "I3_34SorununPuanı", q2responseTime },
            { "I4_35SorununPuanı", q1responseTime },
            { "I5_36SorununPuanı", q2responseTime },
            { "I6_37SorununPuanı", q2responseTime }, 
            { "I7_38SorununPuanı", q2responseTime },
            { "I8_39SorununPuanı", q2responseTime },
            { "I9_40SorununPuanı", q2responseTime },
            { "J0_41SorununPuanı", q2responseTime },  
            { "J1_42SorununPuanı", q2responseTime },
            { "J2_3BÖLÜMTOPLAMPUANI", chapter1Score },
            { "J3_31SorununTepkiSüresi", q1responseTime },
            { "J4_32SorununTepkiSüresi", q2responseTime },
            { "J5_33SorununTepkiSüresi", q1responseTime },
            { "J6_34SorununTepkiSüresi", q2responseTime },
            { "J7_35SorununTepkiSüresi", q1responseTime },
            { "J8_36SorununTepkiSüresi", q1responseTime },
            { "J9_37SorununTepkiSüresi", q1responseTime },
            { "K0_38SorununTepkiSüresi", q1responseTime },
            { "K1_39SorununTepkiSüresi", q1responseTime },
            { "K2_40SorununTepkiSüresi", q1responseTime },
            { "K3_41SorununTepkiSüresi", q1responseTime },
            { "K4_42SorununTepkiSüresi", q1responseTime },
            { "K5_3BÖLÜMTOPLAMTEPKİSÜRESİ", chapter1Score }
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