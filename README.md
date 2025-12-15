# TODO Backend (тебе)

## 0) Подними TCP сервер (.NET 9)
- [ ] Ты создаёшь проект `Server` (.NET 9 Console).
- [ ] Ты поднимаешь `TcpListener` и принимаешь клиентов **асинхронно** (`AcceptTcpClientAsync`).
- [ ] Для каждого клиента ты запускаешь отдельный receive-loop (Task), без блокировок.

## 1) Сделай сущности: клиент и лобби (4 игрока)
- [ ] Ты заводишь `ClientSession`:
  - TcpClient / NetworkStream
  - PlayerId (int/Guid)
  - Nickname, Email
  - IsReady
  - PingMs (опционально)
  - CryptoState (флаг “шифрование включено” + всё, что нужно для Encrypt/Decrypt)
- [ ] Ты заводишь `Room/Lobby` на 4 слота:
  - список игроков (макс 4)
  - Host (первый подключившийся)
  - RoomCode (например DP-P9CR)
- [ ] Если приходит 5-й — ты отправляешь Reject/ServerError и закрываешь соединение.

## 2) Протокол XPacket поверх TCP (обязательно)
- [ ] Ты используешь ваш XProtocol: читаешь из TCP, собираешь байты в буфер, выделяешь пакеты по терминатору и парсишь XPacket.
- [ ] Ты делаешь handshake:
  - до handshake принимаешь/шлёшь только служебные пакеты (без Encrypt)
  - после handshake **все** игровые пакеты отправляешь через `packet.Encrypt()`
  - на приёме после handshake делаешь `Decrypt()` (по правилам вашей библиотеки)
- [ ] Ты документируешь протокол в README: какие типы пакетов и какие поля.

### Минимум типов сообщений (чтобы закрыть требования)
- [ ] `Handshake` / `HandshakeAck` (или аналог из XProtocol)
- [ ] `ClientHello` (nickname + email)
- [ ] `LobbySnapshot` (список игроков: id, nick, ready, host)
- [ ] `ReadyToggle` (или `PlayerReady`)
- [ ] `StartGame`
- [ ] (дальше для игры) `TurnStarted`, `DiceRolled`, `PlayerMoved`, `Win`

## 3) Лобби-логика (что шлёшь в MAUI)
- [ ] После `ClientHello` ты добавляешь игрока в Room и рассылаешь всем `LobbySnapshot`.
- [ ] При дисконнекте игрока — удаляешь его из Room и снова рассылаешь `LobbySnapshot`.
- [ ] При `ReadyToggle` — меняешь ready и снова рассылаешь `LobbySnapshot`.
- [ ] Когда игроков 4 и (как вы решите) все готовы — шлёшь `StartGame`.

### UI контракт (чтобы фронт не мучился)
- [ ] Ты шлёшь `LobbySnapshot` так, чтобы фронт мог просто сделать:
  - `CarouselView.ItemsSource = список_из_4_слотов`
- [ ] В снапшоте ты отдаёшь всегда **ровно 4 слота**:
  - либо `EmptySlot`
  - либо `PlayerSlot { Nick, Ready, Host, Ping }`
- [ ] Порядок слотов ты фиксируешь (например по порядку подключения), чтобы UI не “прыгал”.

## 4) Надёжность сети (важно)
- [ ] Ты учитываешь, что TCP режет/склеивает данные:
  - один пакет может прийти кусками
  - несколько пакетов могут прийти одним куском
  => ты парсишь только после того, как нашёл полный пакет (по терминатору).
- [ ] Ты добавляешь CancellationToken и корректный disconnect/cleanup.
- [ ] Ты не используешь `.Wait()`/`.Result()` — только async.

## 5) Подготовь основу под игровую логику (следующий этап)
- [ ] Ты разделяешь сервисы как на схеме:
  - `GameLoopService` (очередность ходов, победа)
  - `MovementEngine` (движение, стрелки, правило 3 клеток с Пухлёй)
  - `CardProcessor` (пакости/помощь — можно минимально)
- [ ] Ты соблюдаешь правила:
  - особые клетки срабатывают только при движении вперёд
  - стрелки — если остановился на начале стрелки
  - Пухля: подхват по пути, лимит 3 клетки, не двигается назад при откате
  - победа на клетке “Победа”
- [ ] Ты синхронизируешь игру событиями: `TurnStarted`, `DiceRolled`, `PlayerMoved`, `Win`.

## 6) Логи (чтобы быстро чинить)
- [ ] Ты логируешь: connect, handshake ok, hello, lobby snapshot отправлен, ready toggle, start game.
- [ ] Ты добавляешь причину при отказе (комната полная, неверный пакет, и т.д.)

## 7) Где ты трогаешь фронт (минимально)
- [ ] Ты в `MainMenuPage.xaml.cs` вешаешься на `StartButton.Clicked`:
  - берёшь поля ввода
  - connect + handshake + hello
  - переходишь в лобби: `Shell.Current.GoToAsync(nameof(CharacterSelectionPage))`
- [ ] Ты в `CharacterSelectionPage`:
  - по `LobbySnapshot` обновляешь `CarouselView.ItemsSource`
  - по `ConfirmButton.Clicked` шлёшь `ReadyToggle`
  - по `StartGame` перейдёшь на `GamePage` (когда появится)
