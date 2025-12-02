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
        public AudioClip questionAudio2; 
        public int chapterNo;
    }

    public List<QuestionPanel> questionPanels;
    public int currentPanelIndex = 0;
    public float questionTimer = 90f;
    public bool isAnswered = false;
    public bool isQuestionActive = false;

    public List<int> results = new List<int>(); 

    public List<string> questionAnswerTimes = new List<string>();
    public List<double> responseTimes = new List<double>();

    private DatabaseReference databaseReference;

    public AudioSource audioSource;
    public AudioSource nextPageAudioSource;

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
    public DateTime appEndDateTime;
    
    public float pauseStartTime;
    public float totalPauseTime;
    public float questionStartTime;
    public float remainingAudioTime;

    void Start()
    {
        completeScore = 0;
        appStartDay = DateTime.Now.ToString("yyyy-MM-dd");

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

        if (audioSource.isPlaying)
        {
            remainingAudioTime = audioSource.clip.length - audioSource.time;
            audioSource.Pause();
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        totalPauseTime += Time.time - pauseStartTime;

        if (remainingAudioTime > 0 && audioSource.clip != null)
        {
            audioSource.UnPause();
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    void OnStartButtonClicked()
    {
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
        location = locationDropdown.options[locationDropdown.value].text;
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
        if (index == 0)
        {
            appStartTime = DateTime.Now.ToString("HH:mm:ss");
            appStartDateTime = DateTime.Now;
        }
        
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
        
        // if (questionPanels[index].chapterNo == 2)
        // {
        //     foreach (var button in questionPanels[index].allButtons)
        //     {
        //         // button.gameObject.SetActive(false);
        //         // button.onClick.RemoveAllListeners();
        //         // button.onClick.AddListener(() => OnButtonClicked(button));
        //     }
        // }
        if (questionPanels[index].chapterNo == 4) //iki sesli sorular için, chapter değil
        {
            foreach (var button in questionPanels[index].allButtons)
            {
                button.gameObject.SetActive(false);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnButtonClicked(button));
            }
        }
        else
        {
            foreach (var button in questionPanels[index].allButtons)
            {
                button.interactable = false;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnButtonClicked(button));
            }
        }

        questionTimer = 90f;
       
        totalPauseTime = 0f;
        isAnswered = false;
        isQuestionActive = true;

        if (questionPanels[index].questionAudio != null)
        {
            audioSource.clip = questionPanels[index].questionAudio;
            audioSource.Play();
            
            // if (questionPanels[index].chapterNo == 2)
            // {
            //     StartCoroutine(EnableButtonsAfterAudio(questionPanels[index].allButtons, audioSource.clip.length));
            // }
            if (questionPanels[index].chapterNo == 4)
            {
                StartCoroutine(PlayAudioAndEnableButtonsWithSecondAudio(
                    questionPanels[index].questionAudio,
                    questionPanels[index].questionAudio2,
                    questionPanels[index].allButtons
                ));
            }
            else
            {
                StartCoroutine(ActivateButtonsAfterAudio(questionPanels[index].allButtons, audioSource.clip.length));
            }
        }
    }

    IEnumerator EnableButtonsAfterAudio(Button[] buttons, float audioLength)
    {
        float adjustedAudioLength = audioLength;

        while (adjustedAudioLength > 0)
        {
            yield return null;

            if (!isPaused)
            {
                adjustedAudioLength -= Time.deltaTime;
            }
        }

        foreach (var button in buttons)
        {
            button.gameObject.SetActive(true);
        }
        
        questionStartTime = Time.time;
    }

    IEnumerator ActivateButtonsAfterAudio(Button[] buttons, float audioLength)
    {
        float adjustedAudioLength = audioLength;
        while (adjustedAudioLength > 0)
        {
            yield return null;
            if (!isPaused)
            {
                adjustedAudioLength -= Time.deltaTime;
            }
        }

        foreach (var button in buttons)
        {
            button.interactable = true;
        }
        
        questionStartTime = Time.time;
    }
    
    IEnumerator PlayAudioAndEnableButtonsWithSecondAudio(AudioClip questionAudio, AudioClip questionAudio2, Button[] buttons)
    {
        float adjustedAudioLength = questionAudio.length;
        while (adjustedAudioLength > 0)
        {
            yield return null;
            if (!isPaused)
            {
                adjustedAudioLength -= Time.deltaTime;
            }
        }

        foreach (var button in buttons)
        {
            button.gameObject.SetActive(true);
            button.interactable = false; 
        }
        
        if (questionAudio2 != null)
        {
            audioSource.clip = questionAudio2;
            audioSource.Play();

            adjustedAudioLength = questionAudio2.length;
            while (adjustedAudioLength > 0)
            {
                yield return null;
                if (!isPaused)
                {
                    adjustedAudioLength -= Time.deltaTime;
                }
            }
            
            questionStartTime = Time.time;
        }

        foreach (var button in buttons)
        {
            button.interactable = true; 
        }
    }
    
    void OnButtonClicked(Button clickedButton)
    {
        foreach (var button in questionPanels[currentPanelIndex].allButtons)
        {
            button.interactable = false;
        }

        bool isCorrect = clickedButton == questionPanels[currentPanelIndex].correctButton;
        RegisterAnswer(isCorrect);
    }

    void RegisterAnswer(bool isCorrect)
    {
        isAnswered = true;
        isQuestionActive = false;

        float responseTime = Time.time - questionStartTime - totalPauseTime;
        Debug.Log($"responseTime: {responseTime} sec");

        results.Add(isCorrect ? 1 : 0);
        completeScore += isCorrect ? 1 : 0;

        questionAnswerTimes.Add(DateTime.Now.ToString("HH:mm:ss"));
        responseTimes.Add(responseTime);

        nextPageAudioSource.Play();
        StartCoroutine(ShowPanelAfterDelay(currentPanelIndex + 1, 2));
    }

    IEnumerator ShowPanelAfterDelay(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowPanel(index);
    }

    void FinishQuiz()
    {
        Debug.Log("Quiz done! Sending to Firebase...");

        appEndTime = DateTime.Now.ToString("HH:mm:ss");
        appEndDateTime = DateTime.Now;

        TimeSpan elapsedTime = appEndDateTime - appStartDateTime;
        
        fullSessionTime = (appEndDateTime - appStartDateTime).TotalMilliseconds;
        
        Debug.Log("elapsedTime:" + elapsedTime);
        Debug.Log("TotalMilliseconds:" + elapsedTime.TotalMilliseconds);

        var chapter1Score = 0;
        for (int i = 0; i < 7; i++)
        {
            chapter1Score += results[i];
        }

        var chapter1ResponseTime = 0.0;
        for (int i = 0; i < 7; i++)
        {
            chapter1ResponseTime += responseTimes[i];
        }
        
        var chapter2Score = 0;
        for (int i = 7; i < 13; i++)
        {
            chapter2Score += results[i];
        }

        var chapter2ResponseTime = 0.0;
        for (int i = 7; i < 13; i++)
        {
            chapter2ResponseTime += responseTimes[i];
        }

        var completeTime = 0.0;
        for (int i = 0; i < 12; i++)
        {
            completeTime += responseTimes[i];
        }
        
        Debug.Log("complete time" + completeTime);

        WriteData(collectorName, appStartDay, childName, birthday, gender, motherEducation, fatherEducation,
            siblingCount, location,
            locationDetail, getEducationBefore, preEducationTime, appStartTime, appEndTime, fullSessionTime,
            completeScore,
            results[0], results[1], results[2], results[3], results[4], results[5],
            results[6], 
            chapter1Score,
            results[7], results[8], results[9], results[10], results[11], results[12],
            chapter2Score,
            responseTimes[0], responseTimes[1], responseTimes[2], responseTimes[3],
            responseTimes[4], responseTimes[5], responseTimes[6], responseTimes[7],
            chapter1ResponseTime,
            responseTimes[8], responseTimes[9], responseTimes[10], responseTimes[11],
            responseTimes[12],
            chapter2ResponseTime);
    }

    void WriteData(string collectorName, string appStartDay, string childName, string birthday, string gender, string motherEducation,
        string fatherEducation, string siblingCount, string location, string locationDetail, string getEducationBefore,
        string preEducationTime, string appStartTime, string appEndTime, double fullSessionTime, int completeScore,
        int q1Score, int q2Score, int q3Score, int q4Score, int q5Score, int q6Score, int q7Score, int chapter1Score,
        int q8Score, int q9Score, int q10Score, int q11Score, int q12Score, int q13Score, int chapter2Score,
        double q1responseTime, double q2responseTime, double q3responseTime, double q4responseTime, double q5responseTime, 
        double q6responseTime, double q7responseTime, double q8responseTime, double chapter1responseTime,
        double q9responseTime, double q10responseTime, double q11responseTime, double q12responseTime, double q13responseTime, 
        double chapter2responseTime)
    {
        var userData = new Dictionary<string, object>
        {
            { "A0_VeriToplayanKişi", collectorName }, { "A1_UygulamaTarihi", appStartDay }, { "A2_ÇocuğunAdıSoyadı", childName },
            { "A3_ÇocuğunDoğumTarihi", birthday }, { "A4_ÇocuğunCinsiyeti", gender }, { "A5_AnneÖğrenimDurumu", motherEducation },
            { "A6_BabaÖğrenimDurumu", fatherEducation }, { "A7_KardeşSayısı", siblingCount }, { "A8_ÇocuğunYaşadığıİl", location },
            { "A9_ÇocuğunYaşadığıİlİlçeKöy", locationDetail }, { "B0_DahaÖnceOkulÖncesiEğitimiAldımı", getEducationBefore },
            { "B1_DahaÖnceOkulÖncesiEğitimiAlmaSüresi", preEducationTime }, { "B2_UygulamaBaşlamaZamanı", appStartTime },
            { "B3_UygulamaBitişZamanı", appEndTime }, { "B4_ToplamOturumSüresi", fullSessionTime },
            { "B5_TESTİNTAMAMINDANALINANTOPLAMPUAN", completeScore },
            
            // Bölüm1 puan
            { "B6_1SorununPuanı", q1Score }, { "B7_2SorununPuanı", q2Score }, { "B8_3SorununPuanı", q3Score }, { "B9_4SorununPuanı", q4Score },
            { "C0_5SorununPuanı", q5Score }, { "C1_6SorununPuanı", q6Score }, { "C2_7SorununPuanı", q7Score },
            
            { "C3_1BÖLÜMTOPLAMPUANI", chapter1Score },
            
            // Bölüm1 süre
            { "C4_1SorununTepkiSüresi", q1responseTime }, { "C5_2SorununTepkiSüresi", q2responseTime }, { "C6_3SorununTepkiSüresi", q3responseTime },
            { "C7_4SorununTepkiSüresi", q4responseTime }, { "C8_5SorununTepkiSüresi", q5responseTime }, { "C9_6SorununTepkiSüresi", q6responseTime },
            { "D0_7SorununTepkiSüresi", q7responseTime },
            
            { "D1_1BÖLÜMTOPLAMTEPKİSÜRESİ", chapter1responseTime },

            // Bölüm2 puan
            { "D2_8SorununPuanı", q8Score }, { "D3_9SorununPuanı", q9Score }, { "D4_10SorununPuanı", q10Score }, { "D5_11SorununPuanı", q11Score },
            { "D6_12SorununPuanı", q12Score }, { "D7_13SorununPuanı", q13Score }, 
            
            { "D8_2BÖLÜMTOPLAMPUANI", chapter2Score },
            
            // Bölüm2 süre
            { "D9_8SorununTepkiSüresi", q8responseTime }, { "E0_9SorununTepkiSüresi", q9responseTime }, { "E1_10SorununTepkiSüresi", q10responseTime },
            { "E2_11SorununTepkiSüresi", q11responseTime }, { "E3_12SorununTepkiSüresi", q12responseTime },
            { "E4_13SorununTepkiSüresi", q13responseTime }, 
            
            { "E5_2BÖLÜMTOPLAMTEPKİSÜRESİ", chapter2responseTime }
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