private void CheckAndDrain()
{
    var clothing = player.Player.clothing;
    
    // Проверяем очки (Glasses)
    if (clothing.glassesAsset != null && clothing.glassesAsset.vision != ELightingVision.NONE)
    {
        // Тот самый байт [0] из твоего кода!
        bool IsOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;
        
        if (IsOn) 
        {
            DrainItem(ref clothing.glassesQuality, clothing.glassesAsset.id, false);
            if (clothing.glassesQuality == 0)
            {
                // Если разрядилось — выключаем через серверный метод
                // Передаем NON_COSMETIC, так как это функциональный девайс
                clothing.ServerSetVisualToggleState(EVisualToggleType.NON_COSMETIC, false);
            }
        }
    }

    // Аналогично для шапок (Hats)
    if (clothing.hatAsset != null && clothing.hatAsset.vision != ELightingVision.NONE)
    {
        bool IsOn = clothing.hatState != null && clothing.hatState.Length > 0 && clothing.hatState[0] != 0;
        
        if (IsOn) 
        {
            DrainItem(ref clothing.hatQuality, clothing.hatAsset.id, true);
            if (clothing.hatQuality == 0)
            {
                clothing.ServerSetVisualToggleState(EVisualToggleType.NON_COSMETIC, false);
            }
        }
    }
}

private void DrainItem(ref byte quality, ushort id, bool isHat)
{
    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == id);
    float drainRate = config?.DrainPerSecond ?? 0.1f; // Берем из конфига или дефолт

    drainAccumulator += drainRate;
    if (drainAccumulator >= 1f)
    {
        int amount = Mathf.FloorToInt(drainAccumulator);
        drainAccumulator -= amount;
        quality = (byte)Mathf.Max(0, quality - amount);

        // Синхронизация прочности с клиентом
        if (isHat) player.Player.clothing.sendUpdateHatQuality();
        else player.Player.clothing.sendUpdateGlassesQuality();
    }
}
