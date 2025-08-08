# Animation Window UI Injection Testing Guide

## 新功能概述

我們已經實作了將搜尋功能直接整合到 Unity Animation Window 的實驗性功能。這個功能會在 Animation Window 頂部注入一個搜尋框，讓你可以直接在視窗內搜尋和切換動畫片段。

## 測試步驟

### 1. 基本功能測試

1. **開啟 Animation Window**
   - 在 Unity 中開啟 Animation Window (Window > Animation > Animation)
   - 你應該會在視窗頂部看到一個搜尋框，顯示 "Search animation clips (Alt+S)"

2. **搜尋功能測試**
   - 在搜尋框中輸入動畫片段名稱的部分文字
   - 應該會出現一個下拉選單，顯示匹配的結果（最多 10 個）
   - 點擊任一結果應該會切換到該動畫片段

3. **鍵盤快捷鍵測試**
   - 在 Animation Window 中按 `Alt+S` (Mac 上是 `Option+S`)
   - 搜尋框應該獲得焦點並選中所有文字
   - 按 `Enter` 應該選擇第一個搜尋結果
   - 按 `Escape` 應該清除搜尋並隱藏下拉選單

### 2. 相容性測試

1. **多視窗測試**
   - 開啟多個 Animation Window
   - 確認每個視窗都有獨立的搜尋功能

2. **視窗關閉/重開測試**
   - 關閉 Animation Window 後重新開啟
   - 確認搜尋功能仍然正常工作

3. **Unity 重啟測試**
   - 重啟 Unity Editor
   - 確認搜尋功能自動載入

### 3. 錯誤處理測試

1. **無動畫片段測試**
   - 在沒有選擇任何 GameObject 的情況下搜尋
   - 應該顯示專案中所有的動畫片段

2. **降級測試**
   - 如果 UI 注入失敗，按 `Alt+S` 應該開啟原本的彈出式搜尋視窗

### 4. 手動測試命令

如果自動注入沒有生效，可以使用選單命令手動測試：
- `Window > Animation > Test UI Injection`

## 已知限制

1. **Unity 版本相容性**：此功能使用反射來存取 Unity 內部 API，可能在不同版本的 Unity 中表現不同

2. **UI 覆蓋**：搜尋框會佔用 Animation Window 頂部的一些空間

3. **效能考量**：在有大量動畫片段的專案中，搜尋可能會有輕微延遲

## 回報問題

如果遇到任何問題，請記錄：
1. Unity 版本
2. 錯誤訊息（查看 Console）
3. 重現步驟
4. 預期行為 vs 實際行為

## 切換回傳統模式

如果你想暫時停用 UI 注入功能，可以：
1. 註解掉 `AnimationWindowUIInjector.cs` 中的 `[InitializeOnLoad]` 屬性
2. 或者直接使用原本的 `AnimationClipSearchTool` 彈出視窗