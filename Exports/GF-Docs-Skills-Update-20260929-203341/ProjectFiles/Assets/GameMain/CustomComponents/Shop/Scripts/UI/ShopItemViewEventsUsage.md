# ShopItemView View Events 浣跨敤鏂规硶

`ShopItemView` 鐨?`View Events` 鐢ㄦ潵鎶婂晢鍝佸崱鐗囦笂鐨勭壒鏁堛€佹彁绀恒€侀澶栬鏍囩瓑鏄剧ず宸紓鏀惧埌棰勫埗浣?Inspector 涓厤缃紝閬垮厤姣忔柊澧炰竴绉嶅晢鍝佽〃鐜板氨缁欒剼鏈坊鍔犱竴涓瓧娈点€?
## 甯哥敤娴佺▼

1. 鍦ㄥ晢鍝侀厤缃?`ShopItemConfigSO` 涓壘鍒?`View Events / View Tags`銆?2. 娣诲姞涓€涓爣绛撅紝渚嬪 `hot`銆乣limited`銆乣remove_ads_tip`銆?3. 鎵撳紑鍟嗗搧 Item 棰勫埗浣擄紝鎵惧埌 `ShopItemView / View Events`銆?4. 娣诲姞涓€鏉¤鍒欙紝`Match Mode` 閫夋嫨 `ViewTag`锛宍View Tag` 濉悓涓€涓爣绛俱€?5. 鍦?`On Matched` 涓嫋鍏ヨ鏄剧ず鐨勫璞★紝閫夋嫨 `GameObject.SetActive(true)`銆?6. 鍦?`On Unmatched` 涓嫋鍏ュ悓涓€涓璞★紝閫夋嫨 `GameObject.SetActive(false)`銆?
`On Unmatched` 寤鸿涓€瀹氶厤缃€傚晢搴?Item 鍙兘浼氳澶嶇敤锛屽鏋滃彧閰嶇疆鏄剧ず锛屼笉閰嶇疆闅愯棌锛屾棫鍟嗗搧鐣欎笅鐨勭壒鏁堟垨鎻愮ず鍙兘浼氭畫鐣欏埌鏂板晢鍝佷笂銆?
## Match Mode 璇存槑

- `Always`锛氬缁堣Е鍙戯紝閫傚悎鍋氶粯璁ら噸缃€?- `ViewTag`锛氬尮閰?`ShopItemConfigSO.viewTags`锛岄€傚悎涓存椂鎴栫瓥鍒掕嚜瀹氫箟鐨?UI 鏍囪銆?- `PriceType`锛氬尮閰嶄环鏍肩被鍨嬶紝渚嬪閲戝竵銆佸箍鍛娿€佸唴璐€?- `RewardTarget`锛氬尮閰嶅鍔辩被鍨嬶紝渚嬪 `NoAds` 鎴栭亾鍏峰鍔便€?- `Style`锛氬尮閰嶅晢鍝佷娇鐢ㄧ殑 `ShopItemStyleConfigSO`銆?- `ProductId`锛氬尮閰嶅唴璐晢鍝?ID锛屽ぇ灏忓啓涓嶆晱鎰熴€?- `ShowBonus`锛氬尮閰嶆槸鍚︽樉绀?bonus 瑙掓爣銆?- `Visible`锛氬尮閰嶅晢鍝佹槸鍚﹀彲瑙併€?
## 绀轰緥

鏄剧ず鐑棬鐗规晥锛?
- 鍟嗗搧閰嶇疆锛歚View Tags` 娣诲姞 `hot`
- 棰勫埗浣撹鍒欙細
  - `Name`: `Show Hot Effect`
  - `Match Mode`: `ViewTag`
  - `View Tag`: `hot`
  - `On Matched`: `HotEffect.SetActive(true)`
  - `On Unmatched`: `HotEffect.SetActive(false)`

鏄剧ず鍘诲箍鍛婃彁绀猴細

- 涓嶉渶瑕侀澶栨爣绛?- 棰勫埗浣撹鍒欙細
  - `Name`: `Show Remove Ads Tip`
  - `Match Mode`: `RewardTarget`
  - `Reward Target`: `NoAds`
  - `On Matched`: `RemoveAdsTip.SetActive(true)`
  - `On Unmatched`: `RemoveAdsTip.SetActive(false)`

