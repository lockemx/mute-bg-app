using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Windows.Win32;
using Windows.Win32.Media.Audio;
using Windows.Win32.System.Com.StructuredStorage;

namespace MuteAppBG;

public class VolumeMixer
{
    private readonly ISimpleAudioVolume? _simpleAudioVolume;

    public VolumeMixer(string fnToMatch)
    {
        _simpleAudioVolume = GetVolumeObject(fnToMatch);
    }

    public bool Mute
    {
        get
        {
            if (_simpleAudioVolume == null) return false;
            _simpleAudioVolume.GetMute(out var mute);
            return mute;
        }

        set => _simpleAudioVolume?.SetMute(value, Guid.Empty);
    }

    public bool Active()
    {
        return _simpleAudioVolume != null;
    }

    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    private static ISimpleAudioVolume? GetVolumeObject(string fnToMatch)
    {
        var deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

        if (deviceEnumerator == null) return null;
        deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var speakers);

        if (speakers == null) return null;
        var iidIAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
        speakers.Activate(iidIAudioSessionManager2,
            0, new PROPVARIANT(),
            out var o);

        if (o is not IAudioSessionManager2 mgr) return null;
        var sessionEnumerator = mgr.GetSessionEnumerator();
        sessionEnumerator.GetCount(out var count);

        for (var i = 0; i < count; i++)
        {
            sessionEnumerator.GetSession(i, out var ctl);
            var ctl2 = ctl as IAudioSessionControl2;
            ctl2.GetProcessId(out var cPid);

            var fn = "";
            try
            {
                using var p = Process.GetProcessById(Convert.ToInt32(cPid));
                fn = p.ProcessName;
            }
            catch
            {
                // ignored
            }

            if (!fn.Equals(fnToMatch)) continue;
            if (ctl2 is ISimpleAudioVolume volumeControl)
                return volumeControl;
        }

        return null;
    }
}