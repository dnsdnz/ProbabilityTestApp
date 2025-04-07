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
        
        if (questionPanels[index].chapterNo == 2)
        {
            foreach (var button in questionPanels[index].allButtons)
            {
                button.gameObject.SetActive(false);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnButtonClicked(button));
            }
        }
        else if (questionPanels[index].chapterNo == 4)
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
            
            if (questionPanels[index].chapterNo == 2)
            {
                StartCoroutine(EnableButtonsAfterAudio(questionPanels[index].allButtons, audioSource.clip.length));
            }
            else if (questionPanels[index].chapterNo == 4)
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
        for (int i = 0; i < 16; i++)
        {
            chapter1Score += results[i];
        }

        var chapter1ResponseTime = 0.0;
        for (int i = 0; i < 16; i++)
        {
            chapter1ResponseTime += responseTimes[i];
        }
        
        var chapter2Score = 0;
        for (int i = 16; i < 21; i++)
        {
            chapter2Score += results[i];
        }

        var chapter2ResponseTime = 0.0;
        for (int i = 16; i < 21; i++)
        {
            chapter2ResponseTime += responseTimes[i];
        }
        
        var chapter3Score = 0;
        for (int i = 21; i < 31; i++)
        {
            chapter3Score += results[i];
        }

        var chapter3ResponseTime = 0.0;
        for (int i = 21; i < 31; i++)
        {
            chapter3ResponseTime += responseTimes[i];
        }

        var completeTime = 0.0;
        for (int i = 0; i < 31; i++)
        {
            completeTime += responseTimes[i];
        }
        
        Debug.Log("complete time" + completeTime);
        
        WriteData(collectorName, appStartDay, childName, birthday, gender, motherEducation, fatherEducation, siblingCount, location, 
            locationDetail, getEducationBefore, preEducationTime, appStartTime, appEndTime, fullSessionTime, completeScore,
            results[0], results[1], results[2], results[3], results[4], results[5], 
            results[6], results[7], results[8], results[9], results[10], results[11], 
            results[12], results[13], results[14], results[15],
            chapter1Score,
            results[16], results[17], results[18], results[19], results[20], 
            chapter2Score,
            responseTimes[0], responseTimes[1], responseTimes[2], responseTimes[3],
            responseTimes[4], responseTimes[5], responseTimes[6], responseTimes[7],
            responseTimes[8], responseTimes[9], responseTimes[10], responseTimes[11],
            responseTimes[12], responseTimes[13], responseTimes[14], responseTimes[15],
            chapter1ResponseTime,
            responseTimes[16], responseTimes[17], responseTimes[18], responseTimes[19], 
            responseTimes[20], 
            chapter2ResponseTime,
            results[21], results[22], results[23], results[24], results[25],  
            results[26], results[27], results[28], results[29], results[30],
            chapter3Score,
            responseTimes[21], responseTimes[22], responseTimes[23], responseTimes[24], 
            responseTimes[25], responseTimes[26], responseTimes[27], responseTimes[28], 
            responseTimes[29], responseTimes[30],
            chapter3ResponseTime);
    }

    void WriteData(string collectorName, string appStartDay, string childName, string birthday, string gender, string motherEducation,
        string fatherEducation, string siblingCount, string location, string locationDetail, string getEducationBefore,
        string preEducationTime, string appStartTime, string appEndTime, double fullSessionTime, int completeScore,
        int q1Score, int q2Score, int q3Score, int q4Score, int q5Score, int q6Score, int q7Score, int q8Score, int q9Score,
        int q10Score, int q11Score, int q12Score, int q13Score, int q14Score, int q15Score, int q16Score,
         int chapter1Score,
        int q25Score, int q26Score, int q27Score, int q29Score, int q30Score, int chapter2Score,
        double q1responseTime, double q2responseTime, double q3responseTime, double q4responseTime, double q5responseTime, 
        double q6responseTime, double q7responseTime, double q8responseTime, double q9responseTime, double q10responseTime, 
        double q11responseTime, double q12responseTime, double q13responseTime, double q14responseTime,
        double q15responseTime, double q16responseTime, 
        double chapter1responseTime, double q25responseTime, double q26responseTime, double q27responseTime,
        double q29responseTime, double q30responseTime, double chapter2responseTime,
        int q31Score, int q32Score, int q34Score, int q35Score, int q36Score, int q37Score, int q38Score, 
        int q40Score, int q41Score, int q42Score, int chapter3Score, double q31responseTime, double q32responseTime, 
        double q34responseTime, double q35responseTime, double q36responseTime,  double q37responseTime, 
        double q38responseTime, double q40responseTime, double q41responseTime, double q42responseTime,
        double chapter3responseTime)
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
            { "B6_1SorununPuanı", q1Score }, { "B7_2SorununPuanı", q2Score }, { "B8_3SorununPuanı", q3Score }, { "B9_4SorununPuanı", q4Score },
            { "C0_5SorununPuanı", q5Score }, { "C1_6SorununPuanı", q6Score }, { "C2_7SorununPuanı", q7Score }, { "C3_8SorununPuanı", q8Score },
            { "C4_9SorununPuanı", q9Score }, { "C5_10SorununPuanı", q10Score }, { "C6_11SorununPuanı", q11Score }, { "C7_12SorununPuanı", q12Score },
            { "C8_13SorununPuanı", q13Score }, { "C9_14SorununPuanı", q14Score }, { "D0_15SorununPuanı", q15Score }, { "D1_16SorununPuanı", q16Score },
            { "E0_1BÖLÜMTOPLAMPUANI", chapter1Score },
            { "E1_1SorununTepkiSüresi", q1responseTime }, { "E2_2SorununTepkiSüresi", q2responseTime }, { "E3_3SorununTepkiSüresi", q3responseTime },
            { "E4_4SorununTepkiSüresi", q4responseTime }, { "E5_5SorununTepkiSüresi", q5responseTime }, { "E6_6SorununTepkiSüresi", q6responseTime },
            { "E7_7SorununTepkiSüresi", q7responseTime }, { "E8_8SorununTepkiSüresi", q8responseTime }, { "E9_9SorununTepkiSüresi", q9responseTime },
            { "F0_10SorununTepkiSüresi", q10responseTime }, { "F1_11SorununTepkiSüresi", q11responseTime }, { "F2_12SorununTepkiSüresi", q12responseTime },
            { "F3_13SorununTepkiSüresi", q13responseTime }, { "F4_14SorununTepkiSüresi", q14responseTime }, { "F5_15SorununTepkiSüresi", q15responseTime },
            { "F6_16SorununTepkiSüresi", q16responseTime }, 
            { "G5_1BÖLÜMTOPLAMTEPKİSÜRESİ", chapter1responseTime },
            { "G6_25SorununPuanı", q25Score }, { "G7_26SorununPuanı", q26Score }, { "G8_27SorununPuanı", q27Score },
            { "H0_29SorununPuanı", q29Score }, { "H1_30SorununPuanı", q30Score },
            { "H2_2BÖLÜMTOPLAMPUANI", chapter2Score },
            { "H3_25SorununTepkiSüresi", q25responseTime }, { "H4_26SorununTepkiSüresi", q26responseTime }, { "H5_27SorununTepkiSüresi", q27responseTime },
            { "H7_29SorununTepkiSüresi", q29responseTime }, { "H8_30SorununTepkiSüresi", q30responseTime },
            { "H9_2BÖLÜMTOPLAMTEPKİSÜRESİ", chapter2responseTime },
            { "I0_31SorununPuanı", q31Score }, { "I1_32SorununPuanı", q32Score }, { "I3_34SorununPuanı", q34Score },
            { "I4_35SorununPuanı", q35Score }, { "I5_36SorununPuanı", q36Score }, { "I6_37SorununPuanı", q37Score }, { "I7_38SorununPuanı", q38Score },
             { "I9_40SorununPuanı", q40Score }, { "J0_41SorununPuanı", q41Score }, { "J1_42SorununPuanı", q42Score },
            { "J2_3BÖLÜMTOPLAMPUANI", chapter3Score },
            { "J3_31SorununTepkiSüresi", q31responseTime }, { "J4_32SorununTepkiSüresi", q32responseTime }, 
            { "J6_34SorununTepkiSüresi", q34responseTime }, { "J7_35SorununTepkiSüresi", q35responseTime }, { "J8_36SorununTepkiSüresi", q36responseTime },
            { "J9_37SorununTepkiSüresi", q37responseTime }, { "K0_38SorununTepkiSüresi", q38responseTime }, 
            { "K2_40SorununTepkiSüresi", q40responseTime }, { "K3_41SorununTepkiSüresi", q41responseTime }, { "K4_42SorununTepkiSüresi", q42responseTime },
            { "K5_3BÖLÜMTOPLAMTEPKİSÜRESİ", chapter3responseTime }
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