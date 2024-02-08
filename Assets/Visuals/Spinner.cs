using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

[RequireComponent(typeof(Renderer))]
public class Spinner : MonoBehaviour
{
    public float Beam1PeriodMin, Beam1PeriodMax, Beam1MetaPeriod,
        Beam2PeriodMin, Beam2PeriodMax, Beam2MetaPeriod;
    [SerializeField]
    private Color _beamColor;
    public Color BeamColor
    {
        get
        {
            return _beamColor;
        }
        set
        {
            _beamColor = value;
            Material.SetColor("_BeamColor", _beamColor);
        }
    }

    private Material _material;
    private Material Material
    {
        get
        {
            return _material = _material ?? GetComponent<Renderer>().material;
        }
    }

    private float _beam1Position, _beam2Position, _beam1Period, _beam2Period;
    private Color _color1, _color2;

    private Color RandomColor()
    {
        return Random.ColorHSV(150f / 360f, 210f / 360f, 0.4f, 0.75f, 0.65f, 0.85f);
    }

    private float IntervalOf(float period)
    {
        return 1f / period;
    }

    private float SinLerp(float a, float b, float time)
    {
        return Mathf.Lerp(a, b, (Mathf.Sin(time) + 1f) / 2f);
    }

    private void Start()
    {
        BeamColor = _beamColor;
        _color1 = RandomColor();
        _color2 = RandomColor();
        _beam1Position = Random.Range(0f, 1f);
        _beam2Position = Random.Range(0f, 1f);
        _beam1Period = Random.Range(0f, Mathf.PI * 2f);
        _beam2Period = Random.Range(0f, Mathf.PI * 2f);

        Material.SetColor("_Color1", _color1);
        Material.SetColor("_Color2", _color2);
        Material.SetFloat("_Position1", _beam1Position);
        Material.SetFloat("_Position2", _beam2Position);
    }

    private void Update()
    {
        bool flip = Mathf.Abs(_beam1Position - _beam2Position) > 0.5f;
        bool firstGreater = flip ^ _beam1Position > _beam2Position;

        _beam1Period += Time.deltaTime * 2f * Mathf.PI * IntervalOf(Beam1MetaPeriod);
        _beam2Period += Time.deltaTime * 2f * Mathf.PI * IntervalOf(Beam2MetaPeriod);
        _beam1Position += Time.deltaTime * SinLerp(IntervalOf(Beam1PeriodMin), IntervalOf(Beam1PeriodMax), _beam1Period);
        _beam2Position += Time.deltaTime * SinLerp(IntervalOf(Beam2PeriodMin), IntervalOf(Beam2PeriodMax), _beam2Period);
        _beam1Position %= 1f;
        _beam2Position %= 1f;
        if (_beam1Position < 0f) _beam1Position += 1f;
        if (_beam2Position < 0f) _beam2Position += 1f;

        // crossover
        float dist = Mathf.Abs(_beam1Position - _beam2Position);
        if ((dist < 0.2f || dist > 0.8f) && firstGreater ^ dist > 0.5f ^ _beam1Position > _beam2Position)
        {
            if (firstGreater)
            {
                _color2 = _color1;
                _color1 = RandomColor();
            }
            else
            {
                _color1 = _color2;
                _color2 = RandomColor();
            }
            Material.SetColor("_Color1", _color1);
            Material.SetColor("_Color2", _color2);
        }

        Material.SetFloat("_Position1", _beam1Position);
        Material.SetFloat("_Position2", _beam2Position);
    }
}
