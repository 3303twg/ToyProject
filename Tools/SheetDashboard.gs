var TOKEN = 'v7ihi74hv4ihsklneoi8hcas';
var DASHBOARD_NAME = '대시보드';
var TZ = 'Asia/Seoul';
var CACHE_SECONDS = 21600;

function onOpen() {
  SpreadsheetApp.getUi()
    .createMenu('플레이타임')
    .addItem('대시보드 새로고침', 'rebuildDashboard')
    .addItem('10분 자동 갱신 켜기', 'installDashboardTrigger')
    .addToUi();
}

function installDashboardTrigger() {
  var triggers = ScriptApp.getProjectTriggers();
  for (var i = 0; i < triggers.length; i++) {
    if (triggers[i].getHandlerFunction() === 'rebuildDashboard') {
      ScriptApp.deleteTrigger(triggers[i]);
    }
  }
  ScriptApp.newTrigger('rebuildDashboard').timeBased().everyMinutes(10).create();
}

function rebuildDashboard() {
  var lock = LockService.getScriptLock();
  lock.waitLock(30000);
  try {
    refreshDashboard_(SpreadsheetApp.getActive());
  } finally {
    lock.releaseLock();
  }
}

function doGet(e) {
  return handle_(e);
}

function doPost(e) {
  return handle_(e);
}

function handle_(e) {
  try {
    var p = e && e.parameter ? e.parameter : {};
    if (p.token !== TOKEN) return json_({ ok: false, error: 'token' });

    var seconds = Number(p.seconds);
    var game = sanitizeGame_(p.game);
    var install = String(p.install || '').trim();
    var session = String(p.session || '').trim();
    if (!game || !install || !session || !isFinite(seconds) || seconds < 0) {
      return json_({ ok: false, error: 'param' });
    }

    var ended = Number(p.ended);
    var date = isFinite(ended) && ended > 0 ? new Date(ended) : new Date();
    if (isNaN(date.getTime())) date = new Date();

    return json_(record_(game, install, Math.round(seconds), session, date));
  } catch (err) {
    return json_({ ok: false, error: String(err) });
  }
}

function record_(game, install, seconds, session, date) {
  var cache = CacheService.getScriptCache();
  var sessionCacheKey = sessionKey_(game, session);
  var rowCacheKey = rowKey_(game, install);
  var cached = cache.getAll([sessionCacheKey, rowCacheKey]);
  if (cached[sessionCacheKey]) return { ok: true, duplicate: true };

  var ss = SpreadsheetApp.getActive();
  var sheet = ensureGameSheet_(ss, game, cache);
  var row = Number(cached[rowCacheKey] || 0);
  if (row > 1 && applyRow_(sheet, row, install, seconds, session, date, cache, sessionCacheKey, rowCacheKey)) {
    return { ok: true };
  }

  var found = sheet.getRange('A:A').createTextFinder(install).matchEntireCell(true).findNext();
  if (found && found.getRow() > 1 &&
      applyRow_(sheet, found.getRow(), install, seconds, session, date, cache, sessionCacheKey, rowCacheKey)) {
    return { ok: true };
  }

  sheet.appendRow([install, seconds, date, session]);
  var last = sheet.getLastRow();
  if (String(sheet.getRange(last, 1).getValue() || '') === install) {
    cache.put(rowCacheKey, String(last), CACHE_SECONDS);
  }
  cache.put(sessionCacheKey, '1', CACHE_SECONDS);
  return { ok: true };
}

function applyRow_(sheet, row, install, seconds, session, date, cache, sessionCacheKey, rowCacheKey) {
  var values = sheet.getRange(row, 1, 1, 4).getValues()[0];
  if (String(values[0] || '') !== install) return false;
  if (String(values[3] || '') === session) {
    cache.put(sessionCacheKey, '1', CACHE_SECONDS);
    cache.put(rowCacheKey, String(row), CACHE_SECONDS);
    return true;
  }
  sheet.getRange(row, 2, 1, 3).setValues([[(Number(values[1]) || 0) + seconds, date, session]]);
  cache.put(sessionCacheKey, '1', CACHE_SECONDS);
  cache.put(rowCacheKey, String(row), CACHE_SECONDS);
  return true;
}

function ensureGameSheet_(ss, name, cache) {
  var readyKey = ('ready:' + name).slice(0, 200);
  var sheet = ss.getSheetByName(name);
  if (sheet && cache.get(readyKey)) return sheet;

  if (!sheet) {
    try {
      sheet = ss.insertSheet(name);
    } catch (err) {
      sheet = ss.getSheetByName(name);
      if (!sheet) throw err;
    }
  }

  if (String(sheet.getRange(1, 1).getValue()) !== '설치ID') {
    sheet.clear();
    writeGameHeader_(sheet);
    ensureTimezone_(ss);
  }
  cache.put(readyKey, '1', CACHE_SECONDS);
  return sheet;
}

function writeGameHeader_(sheet) {
  sheet.getRange(1, 1, 1, 4).setValues([['설치ID', '누적초', '마지막시각', '마지막세션']]);
  sheet.getRange(1, 1, 1, 4).setFontWeight('bold');
  sheet.setFrozenRows(1);
  sheet.setColumnWidth(1, 280);
  sheet.setColumnWidth(2, 120);
  sheet.setColumnWidth(3, 170);
  sheet.setColumnWidth(4, 280);
  sheet.getRange('B:B').setNumberFormat('#,##0');
  sheet.getRange('C:C').setNumberFormat('yyyy-mm-dd hh:mm:ss');
}

function ensureTimezone_(ss) {
  if (ss.getSpreadsheetTimeZone() !== TZ) ss.setSpreadsheetTimeZone(TZ);
}

function sessionKey_(game, session) {
  return ('s:' + game + ':' + session).slice(0, 200);
}

function rowKey_(game, install) {
  return ('r:' + game + ':' + install).slice(0, 200);
}

function refreshDashboard_(ss) {
  ensureTimezone_(ss);
  var dash = ss.getSheetByName(DASHBOARD_NAME);
  if (!dash) dash = ss.insertSheet(DASHBOARD_NAME, 0);

  var stats = ss.getSheets().filter(isGameSheet_).map(collect_);
  stats.sort(function (a, b) {
    return String(a.name).localeCompare(String(b.name), 'ko');
  });

  var totalUsers = 0;
  var totalToday = 0;
  var totalSeconds = 0;
  for (var i = 0; i < stats.length; i++) {
    totalUsers += stats[i].users;
    totalToday += stats[i].today;
    totalSeconds += stats[i].seconds;
  }

  var rows = [[
    '게임',
    '최초실행횟수',
    '오늘 실행한 기기수',
    '평균 플레이시간',
    '전체 플레이타임'
  ], [
    '전체',
    totalUsers,
    totalToday,
    secondsToDays_(totalUsers ? totalSeconds / totalUsers : 0),
    secondsToDays_(totalSeconds)
  ]];

  for (var r = 0; r < stats.length; r++) {
    var s = stats[r];
    rows.push([
      s.name,
      s.users,
      s.today,
      secondsToDays_(s.users ? s.seconds / s.users : 0),
      secondsToDays_(s.seconds)
    ]);
  }

  dash.clear();
  dash.clearNotes();
  dash.getRange(1, 1, rows.length, 5).setValues(rows);
  dash.setFrozenRows(1);
  if (rows.length > 1) {
    dash.getRange(2, 2, rows.length - 1, 2).setNumberFormat('#,##0');
    dash.getRange(2, 4, rows.length - 1, 2).setNumberFormat('[h]:mm:ss');
  }
  styleDashboard_(dash, rows.length);
  dash.getRange('A1').setNote(
    '오늘 경계는 한국시간. 최초실행횟수는 기기당 한 행. 오늘 실행한 기기수는 마지막 기록이 오늘인 기기. 평균 플레이시간은 누적 시간의 평균. 갱신 ' +
      Utilities.formatDate(new Date(), TZ, 'yyyy-MM-dd HH:mm:ss')
  );

  try {
    ss.setActiveSheet(dash);
    ss.moveActiveSheet(1);
  } catch (ignore) {}
}

function collect_(sheet) {
  var values = sheet.getDataRange().getValues();
  var todayKey = Utilities.formatDate(new Date(), TZ, 'yyyy-MM-dd');
  var installs = {};
  var today = {};
  var seconds = 0;

  for (var i = 1; i < values.length; i++) {
    var install = String(values[i][0] || '').trim();
    var sec = Number(values[i][1]);
    if (!install || !isFinite(sec)) continue;
    if (!installs[install]) installs[install] = true;
    seconds += sec;
    if (dayKey_(values[i][2]) === todayKey) today[install] = true;
  }

  return {
    name: sheet.getName(),
    users: Object.keys(installs).length,
    today: Object.keys(today).length,
    seconds: seconds
  };
}

function isGameSheet_(sheet) {
  if (sheet.getName() === DASHBOARD_NAME) return false;
  return String(sheet.getRange(1, 1).getValue()) === '설치ID';
}

function sanitizeGame_(name) {
  var s = String(name || '').replace(/[\\\/\?\*\[\]:]/g, ' ').replace(/\s+/g, ' ').trim();
  if (!s) s = '이름없음';
  if (s.length > 80) s = s.substring(0, 80);
  if (s === DASHBOARD_NAME) s = s + ' 게임';
  return s;
}

function dayKey_(value) {
  if (Object.prototype.toString.call(value) === '[object Date]' && !isNaN(value.getTime())) {
    return Utilities.formatDate(value, TZ, 'yyyy-MM-dd');
  }
  var match = String(value || '').match(/\d{4}-\d{2}-\d{2}/);
  return match ? match[0] : '';
}

function secondsToDays_(seconds) {
  return Number(seconds) / 86400;
}

function styleDashboard_(dash, rowCount) {
  dash.setColumnWidth(1, 180);
  dash.setColumnWidth(2, 140);
  dash.setColumnWidth(3, 170);
  dash.setColumnWidth(4, 160);
  dash.setColumnWidth(5, 160);

  var header = dash.getRange(1, 1, 1, 5);
  header.setFontWeight('bold');
  header.setBackground('#1f2933');
  header.setFontColor('#ffffff');

  if (rowCount >= 2) {
    dash.getRange(2, 1, 1, 5).setFontWeight('bold').setBackground('#e8f0fe');
  }
  if (rowCount >= 3) {
    dash.getRange(3, 1, rowCount - 2, 5).setBackground('#ffffff');
  }
}

function json_(obj) {
  return ContentService.createTextOutput(JSON.stringify(obj)).setMimeType(ContentService.MimeType.JSON);
}
