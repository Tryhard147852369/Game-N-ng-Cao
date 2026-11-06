using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class DungeonMenuController : MonoBehaviour
{
    public GameObject mainPanel, levelsPanel, settingsPanel, quitPanel, loadingPanel;
    public Slider volumeSlider, sensitivitySlider;
    public TMP_Text volumeValue, sensitivityValue, loadingText;
    public Button secondLevelButton;
    public string firstLevel = "SampleScene", secondLevel = "Level02";
    private bool loading;

    private void Start()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        loadingPanel.SetActive(false);
        Main();
        ReadSettings();
        secondLevelButton.interactable = Application.CanStreamedLevelBeLoaded(secondLevel);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !loading) Main();
    }

    private void ReadSettings()
    {
        volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Volume", .8f));
        sensitivitySlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MouseSensitivity", 100));
        ChangeVolume(volumeSlider.value);
        ChangeSensitivity(sensitivitySlider.value);
        volumeSlider.onValueChanged.AddListener(ChangeVolume);
        sensitivitySlider.onValueChanged.AddListener(ChangeSensitivity);
    }

    // Chỉ bật một bảng để các nút không đè lên nhau.
    private void Show(GameObject panel)
    {
        foreach (var item in new[] { mainPanel, levelsPanel, settingsPanel, quitPanel })
            item.SetActive(item == panel);
    }

    public void Main() { PlayerPrefs.Save(); Show(mainPanel); }
    public void Levels() { Show(levelsPanel); }
    public void Settings() { Show(settingsPanel); }
    public void ConfirmQuit() { Show(quitPanel); }
    public void StartGame() { BeginLoad(firstLevel); }
    public void StartSecondLevel() { BeginLoad(secondLevel); }

    public void ChangeVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
        volumeValue.text = Mathf.RoundToInt(value * 100) + "%";
    }

    public void ChangeSensitivity(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        sensitivityValue.text = Mathf.RoundToInt(value).ToString();
    }

    private void BeginLoad(string scene)
    {
        if (loading || !Application.CanStreamedLevelBeLoaded(scene)) return;
        loading = true;
        PlayerPrefs.Save();
        loadingPanel.SetActive(true);
        StartCoroutine(LoadLevel(scene));
    }

    // Nạp bất đồng bộ để menu vẫn hiển thị tiến độ.
    private IEnumerator LoadLevel(string scene)
    {
        var operation = SceneManager.LoadSceneAsync(scene);
        while (!operation.isDone)
        {
            loadingText.text = "ĐANG VÀO HẦM NGỤC... " + Mathf.RoundToInt(operation.progress / .9f * 100) + "%";
            yield return null;
        }
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
