# NG Trading Web App – System Design Document (SDD)

**Technology**: ASP.NET Core 8/9 – Razor Pages + Tabler UI + SignalR + SQL Server  
**Purpose**: تحويل البوت الحالي (Socket + Money Management + Paper Execution) إلى منصة تداول Web App احترافية بواجهة Dashboard وإدارة إعدادات ومراقبة لحظية.

## 1) الهدف العام

المنظومة تهدف إلى:

- استقبال Signals من Signal Server (TCP Socket) بشكل لحظي.
- تخزين الإشارات في قاعدة البيانات مع Deduplication.
- تشغيل Money Management + Risk Rules لفتح/إغلاق صفقات (Paper Trading أو Real Trading لاحقًا).
- مراقبة الـ PnL لحظيًا وإجراء Auto Exit (Max loss / max hold / take profit).
- عرض كل ذلك في Dashboard احترافية (Capital, Signals, Positions, Logs) مع تحديثات Live.
- دعم Multi-Symbol وMulti-User وMulti-Strategy/Model بالتوسع.

## 2) نطاق النظام

### 2.1 In Scope (النسخة الأولى – Professional MVP)

- Signal Ingestion عبر TCP Socket (hello / status / signal)
- Signals DB: تخزين كل الإشارات، Deduplication حسب (user_id, symbol, interval, candle_time_utc)
- Trading Engine (Paper execution):
  - Open / Hold / Reverse logic كما في الكود الحالي
  - Money management: risk per trade + min trade + max open positions
  - Risk Engine
  - PnL updater loop (كل 3–5 ثواني)
  - Auto exits (time loss pct loss usdt take profit)
- UI Dashboard:
  - Capital summary cards
  - Latest signals table + filters
  - Open positions + close reason + unrealized
  - Execution log timeline
  - Settings page (إعدادات المخاطر)
  - Live UI باستخدام SignalR (بدون Refresh)
- Multi-currency support: أي Symbol من Binance Futures (BTCUSDT, ETHUSDT… إلخ)

### 2.2 Out of Scope (يُضاف لاحقًا)

- تنفيذ صفقات حقيقية على Binance (Real Trading) — (يُضاف عبر Exchange Connector)
- إدارة مفاتيح API و Permissions وسياسات أمان متقدمة
- Backtesting UI
- ML training داخل نفس النظام (سنجهز له Hooks)

## 3) المتطلبات غير الوظيفية (Non-Functional)

- **Reliability**: إعادة الاتصال التلقائي بالسيرفر TCP
- **Performance**:
  - تحديث PnL ≤ 5 ثواني
  - UI Live بدون ضغط (SignalR throttling عند الحاجة)
- **Maintainability**: فصل المنظومة لخدمات واضحة (Workers + Repos + Engine)
- **Observability**: Logs + metrics (مستقبلاً Prometheus)
- **Security**:
  - عدم تخزين أسرار (لو Paper فقط)
  - لو Real: تخزين API keys مشفر + least privilege
  - حماية صفحات Settings / Admin

## 4) المعمارية العامة

### 4.1 Overview

النظام يتكون من 4 طبقات:

1. Web UI (Razor Pages + Tabler)
2. Real-time Layer (SignalR Hub)
3. Background Workers (Hosted Services)
4. Data Layer (SQL Server + Repositories)

### 4.2 Core Services

**A) SignalSocketWorker**

- Connect to TCP signal server
- Parse JSON
- Upsert signals
- Trigger trading engine decisions (للـ type=signal فقط)
- Broadcast updates to UI via SignalR

**B) PnlWorker**

- Poll open positions
- Fetch last price for required symbols
- Update unrealized pnl and risk rules
- Force close if needed (transaction-safe)

**C) BinancePriceClient**

- Call Binance futures ticker endpoint
- Optional caching + retry + timeout policy

**D) TradingEngine**

- DecideAndExecute():
  - candidate && (Buy/Sell) && entry_price exists
  - max_open_positions
  - capital available
  - open/reverse logic
  - DB transaction Serializable

## 5) تدفق البيانات (Data Flow)

### 5.1 Signal Flow

Socket receives line: `{type:"signal", data:[...] }`

لكل signal object:

1. Validate
2. Upsert into signals
3. If type=signal:
   - TradingEngine.DecideAndExecute(signalId, signal)
   - Insert into execution_log
4. SignalR push events:
   - signal_received
   - position_changed
   - capital_changed
   - log_added

### 5.2 PnL Flow

كل 5 ثواني:

1. Load open positions
2. Build symbols set
3. Fetch last prices per symbol
4. Update positions unrealized fields
5. Evaluate risk exits:
   - if triggered:
     - close position in transaction
     - update capital
     - log + broadcast

## 6) قاعدة البيانات (Database Design)

أنت لديك tables بالفعل. هنا “التوسعة الاحترافية” لدعم multi-currency + multi-strategy + multi-user.

### 6.1 Tables الأساسية

1) **users**

- user_id (PK)
- name
- role (Admin/User)
- created_utc

2) **capital**

- user_id (PK/FK)
- equity
- available
- reserved
- updated_utc

3) **signals**

- signal_id (PK)
- user_id (FK)
- symbol
- interval
- candle_time_utc
- model_signal (Buy/Sell/Hold/NONE)
- confidence
- entry_price (nullable)
- candidate (bit)
- blocked_by (nullable)
- pivot_ok (nullable)
- raw_json (nvarchar(max))
- received_utc

Unique Index: (user_id, symbol, interval, candle_time_utc)  
✅ يمنع التكرار ويدعم MERGE

4) **positions**

- position_id (PK)
- user_id (FK)
- symbol
- side (LONG/SHORT)
- qty
- entry_price
- entry_time_utc
- status (OPEN/CLOSED)
- exit_price (nullable)
- exit_time_utc (nullable)
- pnl (nullable)
- close_reason (nullable) (REVERSE / MANUAL / RISK / TP / SL …)
- reserved_margin
- last_price (nullable)
- unrealized_pnl (nullable)
- unrealized_pnl_pct (nullable)
- last_mark_utc (nullable)
- risk_exit_reason (nullable)

5) **execution_log**

- log_id (PK)
- user_id
- signal_id (nullable)
- symbol
- decision (EXECUTED/SKIPPED)
- reason (nullable)
- action (nullable)
- created_utc

### 6.2 جداول توسعة احترافية

6) **strategies**

- strategy_id (PK)
- name (e.g., “RF_1h_v3”)
- is_active
- created_utc

7) **user_strategy_settings**

- user_id
- strategy_id
- risk_per_trade_pct
- min_trade_usdt
- max_open_positions
- max_holding_seconds
- max_loss_pct
- max_loss_usdt
- take_profit_pct
- symbols_scope (nullable JSON) // قائمة رموز معينة
- intervals_scope (nullable JSON)
- updated_utc

8) **symbol_state** (اختياري)

- user_id
- symbol
- last_signal_time
- last_trade_time
- cooldown_seconds
- flags_json

## 7) واجهة المستخدم (UI/UX) – Tabler Layout

### 7.1 Pages

- **/Dashboard**
  - Cards: Equity / Available / Reserved / Open Positions count
  - Latest Signals (table + badge Buy/Sell)
  - Live “Activity Feed” من execution_log
- **/Signals**
  - Filters: symbol, interval, candidate, model_signal, date range
  - Table + raw_json viewer (modal)
- **/Positions**
  - Open positions table
  - Closed positions history
  - Button “Manual Close” (Paper) مع confirmation
  - PnL charts (later)
- **/Logs**
  - Execution logs table + filters
- **/Settings**
  - إعدادات المخاطر
  - Symbols selection (multi-select)
  - Save to DB (strategy/user settings)
- **/Admin** (اختياري)
  - Users management
  - Strategies list
  - System health

### 7.2 Live Updates (SignalR Events)

- signal_received → إضافة صف جديد في signals table
- position_opened/closed/updated → تحديث positions
- capital_updated → تحديث cards
- log_added → activity feed

## 8) الـ Trading Rules (كما هو + تحسينات مقترحة)

### 8.1 Current Rules (موجودة)

- Candidate = true
- model_signal = Buy/Sell
- entry_price exists
- Max open positions global
- reserve = max(equity*risk_pct, min_trade_usdt)
- qty = reserve / entry_price
- If open position exists:
  - same direction → skip
  - opposite → reverse (close + open)

### 8.2 Enhancements (احترافية)

- Per-symbol max positions (مثلاً 1 position per symbol = OK)
- Cooldown بعد Reverse (مثلاً 10 دقائق ممنوع يفتح تاني على نفس العملة)
- Spread/Slippage simulation (Paper execution realism)
- Leverage model (Paper) (اختياري) لتقريب futures
- Risk per strategy بدل global constants

## 9) دعم أكتر من عملة (Multi-Currency) بشكل صحيح

النظام أساسًا multi-symbol لأن symbol جزء من:

- signal key
- positions rows
- price fetching set

تحسينات لتوسعة كبيرة:

- Price caching per symbol لمدة 1–2 ثانية لتقليل HTTP calls
- Rate limit handling من Binance:
  - backoff
  - use batch endpoints (لاحقًا)
- تقسيم PnL worker:
  - shard by user or by symbol groups

## 10) التوسع لتنفيذ حقيقي (Real Trading) — Architecture جاهزة

ضيف طبقة Exchange Connector Interface:

### 10.1 Interface

- IExchangeClient.PlaceOrder(symbol, side, qty, type, price?)
- IExchangeClient.GetOpenPositions()
- IExchangeClient.ClosePosition(symbol or positionId)
- IExchangeClient.GetBalance()

### 10.2 Implementations

- PaperExchangeClient (اللي عندك)
- BinanceFuturesClient (حقيقي لاحقًا)

### 10.3 Safety (Real Trading)

- API key permissions: Futures trade only
- Max daily loss limit
- Kill switch
- Manual approval mode (semi-auto)

## 11) توسيع الموديل/الـ Signal Layer (ML/Models)

### 11.1 Current

Signal server يرسل:

- model_signal
- confidence
- candidate
- entry_price
- pivot_ok

### 11.2 What to Add (لو ناقص)

- stop_loss_price (اختياري)
- take_profit_price (اختياري)
- risk_score (0-100)
- features snapshot (light JSON) للتتبع
- model_version + strategy_id

### 11.3 Why?

- تتبع الأداء حسب version
- A/B testing
- تحليل أخطاء وإعادة تدريب

## 12) التشغيل والنشر (Ops)

### 12.1 Local Deployment

- IIS Express / Kestrel
- SQL Express
- signal server on localhost

### 12.2 Production

- Windows Server + IIS (Reverse Proxy) أو Kestrel + NSSM
- SQL Server Standard
- Logging:
  - Serilog + rolling files
- Health checks endpoint:
  - socket connection status
  - DB connectivity
  - last PnL update timestamp

## 13) الأمان (Security)

- **Authentication (لـ UI)**:
  - ASP.NET Core Identity أو Windows Auth حسب بيئتك
- **Authorization**:
  - Admin pages restricted
- **SQL injection**:
  - ADO.NET parameters (أنت شغال صح)
- **Secrets**:
  - appsettings.Secrets.json أو Windows Credential Manager (لو Real trading)

## 14) أخطاء/ملاحظات على الكود الحالي (تتحسن في التحويل)

- MAX_HOLDING_SECONDS = 6 * 360099 خطأ واضح (لازم 6*3600)
- RISK_PER_TRADE_PCT عندك مكتوب 0.5 ومع تعليق “2%” → لازم توحيد
- Binance price fetch per symbol بدون retry/backoff — نضيف policies
- Socket loop محتاج cancellation token بدل volatile bool (في HostedService)
- فصل الـ DB logic في Repositories بدل Program.cs (تحسين maintainability)

## 15) خطة التنفيذ (Implementation Plan)

**Phase 1 — تحويل أساسي (1)**

- إنشاء Razor Pages + Tabler layout
- نقل DB code إلى Repos
- SignalSocketWorker يعمل Upsert + logs
- Dashboard يعرض capital + signals

**Phase 2 — Trading Engine (2)**

- إضافة TradingEngine service
- تنفيذ DecideAndExecute transaction-safe
- Positions page + logs page

**Phase 3 — PnL & Risk (3)**

- PnlWorker + price client caching
- risk exits + force close
- SignalR live updates لكل التغييرات

**Phase 4 — Multi-user + Strategy Settings (4)**

- users table
- user_strategy_settings
- Settings UI يكتب DB

**Phase 5 — Real Trading (اختياري)**

- IExchangeClient
- Binance connector + safety

## 16) Deliverables النهائية (احترافية)

- Solution جاهز:
  - CryptoBot.Web (Razor + Tabler)
  - CryptoBot.Core (Engine + Models)
  - CryptoBot.Data (Repos)
- SQL scripts:
  - create/alter tables + indexes
- تشغيل:
  - README تشغيل محلي + إعدادات
- UI:
  - Dashboard + Signals + Positions + Logs + Setting
