using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Отображение таймера — вынесено из GameManager (SRP).</summary>
public sealed class GameTimerPresenter
{
    readonly TextMeshProUGUI _timerText;
    readonly Image _timerBar;
    readonly Slider _timerSlider;
    Image _candleWaxFill;
    readonly Color _normalColor;
    readonly Color _warningColor;
    readonly float _warningThreshold;
    readonly bool _showDigital;
    readonly float _timeBonusMaxMultiplier;

    public GameTimerPresenter(
        TextMeshProUGUI timerText,
        Image timerBar,
        Slider timerSlider,
        Image candleWaxFill,
        Color normalColor,
        Color warningColor,
        float warningThreshold,
        bool showDigitalTimer,
        float timeBonusMaxMultiplier)
    {
        _timerText = timerText;
        _timerBar = timerBar;
        _timerSlider = timerSlider;
        _candleWaxFill = candleWaxFill;
        _normalColor = normalColor;
        _warningColor = warningColor;
        _warningThreshold = warningThreshold;
        _showDigital = showDigitalTimer;
        _timeBonusMaxMultiplier = timeBonusMaxMultiplier;
    }

    public void SetCandleWaxFill(Image wax) => _candleWaxFill = wax;

    public void ConfigureSlider(float gameDuration, float currentTime)
    {
        if (_timerSlider == null) return;
        float cap = gameDuration * Mathf.Max(1f, _timeBonusMaxMultiplier);
        _timerSlider.minValue = 0f;
        _timerSlider.maxValue = Mathf.Max(1f, cap);
        _timerSlider.wholeNumbers = false;
        _timerSlider.interactable = false;
        _timerSlider.value = Mathf.Max(0f, currentTime);
    }

    public void Refresh(float currentTime, float gameDuration)
    {
        float t = Mathf.Max(0f, currentTime);
        if (_showDigital && _timerText != null)
        {
            int minutes = Mathf.FloorToInt(t / 60f);
            int seconds = Mathf.FloorToInt(t % 60f);
            _timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            _timerText.color = t <= _warningThreshold ? _warningColor : _normalColor;
        }

        float timeCap = gameDuration * Mathf.Max(1f, _timeBonusMaxMultiplier);
        float barFullDenom = Mathf.Max(0.001f, gameDuration);
        float ratio = Mathf.Clamp01(t / barFullDenom);

        if (_timerBar != null && _timerBar.gameObject.activeSelf)
        {
            _timerBar.fillAmount = ratio;
            _timerBar.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.3f, 0.8f, 0.3f), ratio);
        }

        if (_candleWaxFill != null)
            _candleWaxFill.fillAmount = ratio;

        if (_timerSlider != null && gameDuration > 0f)
        {
            _timerSlider.maxValue = timeCap;
            _timerSlider.value = t;
        }
    }
}
