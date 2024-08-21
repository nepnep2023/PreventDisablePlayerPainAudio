using System;
using System.Diagnostics;
using System.Collections.Generic;
using GTA;


unsafe public class PreventDisablePlayerPainAudio : Script
{
    Ped currentPed = null;
	bool currentPedGet = false;

	List<ulong> disablePainAddrs = new List<ulong>();

	bool painAddrGetStart = true;


	public PreventDisablePlayerPainAudio()
	{
		Tick += OnTick;
	}
	void OnTick(object sender, EventArgs e)
	{
		if (Game.IsLoading)
		{
			currentPedGet = false;
		}
		if (!Game.IsLoading && !Game.IsPaused)
        {
            if (currentPedGet && Game.Player.Character.Exists() && currentPed.Exists() && currentPed != Game.Player.Character)
            {
                currentPedGet = false;
            }
            if (!currentPedGet || !currentPed.Exists())
			{
				currentPed = Game.Player.Character;
				currentPedGet = true;
				painAddrGetStart = true;
			}
			if (painAddrGetStart && Game.Player.CanControlCharacter)
			{
				disablePainAddrs.Clear();

                ProcessModule module = Process.GetCurrentProcess().MainModule;
                ulong address = (ulong)module.BaseAddress.ToInt64();
                ulong endAddress = address + (ulong)module.ModuleMemorySize;

				string pattern = "\x80\x00\x7F\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x3E\x00\x00\x40\x04\x00\x00\x00";
				string mask = "xxxxxxxx??????xxxxxxxxxx";
				
				ulong[] baseLocation = FindPatterns(address, pattern, mask, endAddress, false);
                
				if (baseLocation != null)
				{
					ulong baseLocationAddr = *(ulong*)(baseLocation[0] + 0x8);

                    //↓3.0で導入してみたアドレス取得過程
					pattern = "\x01\x00\x00\x08\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\xFF\xFF\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x64\x00\x00\x00\xFF\xFF\xFC\xFF";
					mask = "xxxx??????xx??xx????xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx????????xxxxxxxx????x???xxxx";
					
					ulong[] baseAddr = FindPatterns(baseLocationAddr + 0xC, pattern, mask, baseLocationAddr + 0x3E800000, true);　//プレイヤーpedだけでなく、その時点で存在する全てのpedのpainオフアドレスを取得するみたい？
					if (baseAddr != null)
					{
						foreach (ulong addr in baseAddr)
						{
							disablePainAddrs.Add(addr + 0x55A);
						}
					}
                }

                painAddrGetStart = false;
			}
			Wait(0);

			if(disablePainAddrs!= null && disablePainAddrs.Count > 0)
			{
#if DEBUG
				GTA.UI.Notification.Show("painオフアドレスは取得されている", false);
                GTA.UI.Notification.Show("取得されたアドレス数" + disablePainAddrs.Count, false);

#endif
                foreach (ulong addr in disablePainAddrs)
                {                    
					if (*(byte*)addr == 1) //ここで操作する数値の値が1であるかを確認しておかないと、消滅したpedのpainオフアドレスも操作してしまい結果GTA5がクラッシュする　してもクラッシュするかもだけど、これを試しに無効化するまではほぼ無かった
					{
						*(byte*)addr = 0;
#if DEBUG
						GTA.UI.Notification.Show("painオフアドレスを操作をしている", false);
#endif
					}
                }
            }
			else
			{
				Wait(5000);
				painAddrGetStart = true;
#if DEBUG
				GTA.UI.Notification.Show("painオフアドレスを取得していない", false);
#endif
			}
#if DEBUG
			GTA.UI.Notification.Show("Pain Audio 有効化作動中", false);
#endif
			Game.Player.Character.IsPainAudioEnabled = true;
		}
	}
		
	ulong[] FindPatterns(ulong address, string pattern, string mask, ulong endAddress, bool secondFind)
	{

        List<ulong> check = new List<ulong>();

		for (; address < endAddress; address += 0x10)
		{
			for (int i = 0; i < pattern.Length; i++)
			{
				if (mask[i] != '?' && ((byte*)address)[i] != pattern[i])
				{
					break;
				}
				else if (i + 1 == pattern.Length)
				{
#if DEBUG
					GTA.UI.Notification.Show("パターン合致", false);
#endif
                    Wait(20);
                    check.Add(address);
					if (!secondFind)
					{
						return check.ToArray();
					}
					break;
				}
			}
		}
		if (check.Count > 0)
		{
			return check.ToArray();
		}
#if DEBUG
		GTA.UI.Notification.Show("パターン発見できず", false);
		Wait(250);
#endif
		return null;
	}
}
