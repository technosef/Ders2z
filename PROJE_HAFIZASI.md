# Ders Dağıtım Uygulaması — Proje Hafızası

Bu dosya kalıcı çalışma bağlamıdır. Yeni geliştirme turunda önce bu dosya ve `ILERLEME_PLANI.md` okunur; her doğrulanmış değişiklikten sonra ikisi birlikte güncellenir.

## Değişmez Proje Kararları

- Proje kökü: `C:\ikizsoft\Ders2z`
- Ürün: C# / WPF tabanlı normal Windows masaüstü uygulaması
- Yerel veri: SQLite; XAMPP ana ürünün parçası değildir
- Kullanıcı arayüzü: Türkçe, sade ve Office/aSc iş akışına alışık idareciyi zorlamayacak yapı
- Ana akış: Tanımlar → Kurallar/Kilitler → Taslak Üret → Çakışma Kontrolü → Çizelgeler → Rapor/Excel/PDF/XML
- Otomatik üretim açıklanabilir taslak üretir; manuel değişiklikleri ve kesin kilitleri korur
- Mevzuat ve geçiş koşulları sabit kodlanmaz; düzenlenebilir kural/veri olarak tutulur
- Gerçek kurum verisinin ana kaynağı ASC XML aktarımıdır; demo seed yeniden çalıştırılmaz

## Çözüm Yapısı

- `DersDagitim.Domain`: öğretmen, sınıf, ders, kaynak, program türü/geçişi, görev, uygunluk ve atama modelleri
- `DersDagitim.Application`: repository sözleşmeleri, ASC parser/eşleme, açıklanabilir greedy solver
- `DersDagitim.Infrastructure`: SQLite şeması, CRUD, ASC import/export ve kart override kalıcılığı
- `DersDagitim.Wpf`: ana navigasyon, veri yönetimi, taslak, mevcut ASC programı, genel görünüm, rapor ve uygunluk ekranları

## Canlı Veri Durumu — 8 Ağustos 2026

- Canlı veritabanı: `C:\Users\masco\AppData\Local\DersDagitim\ders-dagitim.db`
- ASC envanteri: 1 import, 2.166 ham düğüm, 145 grup, 522 lesson talebi, 1.259 card
- Kullanıcı tabloları: 44 öğretmen, 121 ders, 46 sınıf/şube, 46 oda/kaynak
- Solver eşlemesi: 522/522 talep ve 1.259/1.259 kart
- Demo kayıtları temizlenmiş durumda; Mehmet Akif Sönmez tek ve normal öğretmen kaydıdır
- Öğretmen renkleri ve ASC kart renkli gösterimi korunur

## Son Tamamlanan Teknik İşler

- `AscSolverInput`, solver ataması ile gerçek `AscCards.Id` arasındaki bağı taşıyor.
- `AscCardOverrides` taşıma/kaldırma durumları solver girdisine uygulanıyor.
- Taslak ekranında taşı, kaldır ve son kaldırmayı geri al işlemleri SQLite'a yazılıyor.
- Yeniden `Taslak Üret` çalışınca kalıcı override'lar korunuyor.
- Taşıma öncesi sınıf, öğretmen, kaynak kapasitesi, blok sınırı ve hard-lock kontrolü yapılıyor.
- Gerçek veritabanında kopmuş ASC tanım kimlikleri, doğrulanmış `asc-map.db` içeriğinden transaction ile onarıldı.
- Onarım öncesi yedek: `C:\ikizsoft\Ders2z\backups\ders-dagitim-before-asc-relation-repair-20260808-2.db`
- Kaynak/kaydetme akışında `Resources.IsDemo=0` düzeltildi.
- Ana `Taslak Üret` handler'ı eski demo üretimi yerine doğrudan gerçek ASC akışına bağlandı.

## Son Doğrulamalar

- Çözüm derlemesi: başarılı, 0 hata, 0 uyarı
- Veri testi: taşı → yeniden oku → kaldır → yeniden oku → geri al → yeniden oku başarılı
- Test sonucu: 522 request, 1.259 mapped card, 1.259 protected card
- EXE açılışı daha önce doğrulandı; son gerçek-handler değişikliğinin tam UI otomasyon testi henüz tamamlanmadı
- Test sırasında başlatılan WPF süreçleri kapatıldı; çalışan `DersDagitim.Wpf` süreci bırakılmadı

## Bilinen Riskler / Dikkat Edilecekler

- İlk kullanıcı kaynak yolu `C:\Users\masco\Desktop\asctt2012 (2) (1).xml` artık mevcut değil.
- İlişkileri doğru koruyan yerel doğrulama kaynağı `C:\ikizsoft\Ders2z\asc-map.db` dosyasıdır; silinmemelidir.
- Mevcut ASC exporter, tanım kimliklerini çekirdek GUID'lerle yeniden üretmemelidir. Dışa aktarımda orijinal ASC external ID'leri koruyan eşleme ayrıca sertleştirilecek.
- `MainWindow.xaml` yan panelindeki eski “DEMO VERİ” açıklaması kurum verisi metnine çevrilmelidir.
- `bin/` ve `obj/` dosyaları git tarafından izleniyor; kullanıcı değişiklikleri olduğu için toplu silme/reset yapılmayacak.
- Kök dizindeki `deneme.pdf` kullanıcı dosyası kabul edilir; dokunulmaz.

## Sıradaki Kesin İşler

1. Gerçek ASC `Taslak Üret` düğmesini UI otomasyonunda 522/1.259 sayılarıyla doğrula.
2. Taslakta gerçek kart seçerek taşı/kaldır/geri al düğme akışını UI üzerinden doğrula.
3. Ana ekrandaki kalan demo metnini kaldır ve veri durumu panelini gerçek sayımlarla tutarlılaştır.
4. ASC export'ta subject/teacher/class/classroom external ID'lerini koru; override'ları cards çıktısına uygula.
5. Export → yeniden import round-trip testinde 145/522/1.259 ilişkilerini doğrula.
6. Uçtan uca manuel düzenleme, çakışma, genel görünüm ve Excel/PDF/XML testini tamamla.

## Çalışma Kuralı

- Kullanıcı “devam” dediğinde sıfırdan keşif yapılmaz; önce bu dosyadaki sıradaki kesin işten başlanır.
- Gerçekten bitmeyen adım ✅ yapılmaz.
- Veri değiştiren migration/import öncesi yedek ve transaction kullanılır.
- Kullanıcıya her somut sonuç kısa `✅ / 🔄 / ⬜` özetiyle bildirilir.

## 9 Ağustos 2026 Devam Notu

- ✅ `MainWindow.xaml` ve `DataManagementWindow.xaml` içindeki görünür demo/örnek veri metinleri kurum ASC verisi diline çevrildi.
- ✅ `SchoolOverviewWindow` sabit filtrelerden çıkarıldı; alan, program, sınıf/şube, öğretmen ve kaynak filtreleri gerçek sınıf tablosu ve `GetAscScheduleCardsAsync()` kartlarından dolduruluyor.
- ✅ Okul genel görünümü artık demo özet satırı üretmiyor; gerçek ASC kartlarını gün/saat/sınıf/ders/öğretmen/kaynak ve manuel durum etiketiyle listeliyor.
- ✅ `ExportAscXmlAsync()` çağrı yolu, subject/teacher/class/classroom bölümlerini içe aktarılan ham ASC node external ID'lerinden yazıyor; çekirdek GUID'lerle yeniden üretmiyor.
- ✅ ASC export `AscCardOverrides` kayıtlarını cards çıktısına uyguluyor: taşınan kartta period/days değişiyor, kaldırılan kart export dışında kalıyor.
- ✅ Canlı export round-trip doğrulaması: 145 grup, 522 lesson, 1.259 card; subject/teacher/class/classroom ID örneklerinde GUID benzeri değer sayısı 0.
- ✅ Kopya veritabanında override doğrulaması: bir kart taşındı, bir kart kaldırıldı; export kart sayısı 1.258 oldu ve taşıma çıktıya uygulandı.
- ✅ `dotnet build DersDagitim.sln`: başarılı, 0 hata, 0 uyarı.
- ✅ EXE smoke: WPF uygulaması 6 saniye canlı kaldı ve kapatıldı.
- ✅ Yerel git commit: `dad5979` (`Gercek veri ekranlari ve hakkinda paneli`).
- ⚠️ Git push yapılamadı; repoda `origin` remote tanımlı değil.
- ✅ EXE smoke: `DersDagitim.Wpf.exe` başlatıldı, 6 saniye canlı kaldı ve kapatıldı.
- Not: Round-trip doğrulama için `tmp/RoundTrip` geçici harness'i ve `tmp/roundtrip-*.xml/db` çıktıları oluşturuldu; canlı `deneme.pdf` ve ilgisiz kullanıcı dosyalarına dokunulmadı.

## 9 Ağustos 2026 Kullanıcı Ekran Düzeltmeleri

- ✅ `TeacherAvailabilityWindow` boş tablo yerine gerçek öğretmenleri "kısıt tanımlı değil" satırıyla gösterir hale getirildi; bu ekran mevcut durumda ders programı değil, öğretmen uygunluk/kilit kayıt ekranıdır.
- ✅ `DraftScheduleWindow` okul geneli aynı saat ders sayılarını artık çakışma gibi turuncu göstermiyor; çoklu okul dersleri mavi yoğunluk hücresi olarak gösteriliyor ve açıklama metni düzeltildi.
- ✅ `ConflictWindow` yalnız yerleşmeyen talepleri değil, gerçek sınıf/öğretmen/kaynak bindirmelerini de hesaplayıp listeliyor.
- ✅ `TeacherReportWindow` örnek `TeacherReportSample` verisinden çıkarıldı; gerçek ASC/taslak atamalarından öğretmen seçilebilir ders yükü raporu üretir hale getirildi.
- ✅ Bu kullanıcı odaklı ekran düzeltmelerinden sonra `dotnet build DersDagitim.sln` ve WPF EXE smoke testi tekrar başarılı.

## 9 Ağustos 2026 Ek Rapor Düzeltmesi

- ✅ `TeacherReportSample` domain sınıfı kaldırıldı; öğretmen raporu artık örnek Mehmet Akif verisine geri düşmez.
- ✅ Son derleme ve EXE smoke testi tekrar başarılı.

## 9 Ağustos 2026 Manuel Kullanım ve Okul Geneli Düzeltmeleri

- ✅ `SchoolOverviewWindow` duplicate sınıf adı durumunda hata vermeyecek şekilde düzeltildi; sınıf eşlemesi `GroupBy(...).First()` ile dayanıklı hale getirildi.
- ✅ Okul genel görünümüne "Renkli okul dosyası" HTML export eklendi; tüm okulun gün/saat bazlı renkli programı tarayıcı/PDF için üretilebilir.
- ✅ `DraftScheduleWindow` içine sınıf ve öğretmen filtreleri eklendi; filtre seçildiğinde hücreler tek ders seviyesine iner ve mevcut sürükle-bırak gün/saat taşıma ile seçili atamayı kaldırma kullanılabilir hale gelir.
- ✅ Taslak CSV çıktısı sınıf/ders/öğretmen adlarını yazar hale getirildi.
- ✅ `dotnet build DersDagitim.sln` ve WPF EXE smoke testi başarılı.
- ⚠️ Dersi başka öğretmene kalıcı devretme henüz ayrı veri modeli gerektiriyor. Mevcut `AscCardOverrides` sadece gün/saat/kaldırma saklıyor; öğretmen devri için `CardId -> Teacher` override tablosu, solver girdi uygulaması ve ASC export teacherids güncellemesi eklenmeli.

## 9 Ağustos 2026 Sınıf ve Kaynak Program Çıktıları

- ✅ `LaboratoryScheduleWindow` demo laboratuvar örneğinden çıkarıldı; gerçek ASC/taslak atamalarından çalışan "Sınıf ve Kaynak Haftalık Programları" ekranına dönüştürüldü.
- ✅ Ekranda `Sınıf` ve `Kaynak/Lab` modu eklendi; sınıf seçerek sınıf haftalık programı, kaynak/laboratuvar seçerek kaynak haftalık programı izlenebilir.
- ✅ Sınıf/kaynak programları CSV dışa aktarım ve PrintDialog üzerinden PDF/yazdırma desteği aldı.
- ✅ Ana menüde `Çizelgeler` butonu artık bu sınıf/kaynak program ekranını açıyor; `Taslak Üret` taslak düzenleme ekranını açmaya devam ediyor.
- ✅ `dotnet build DersDagitim.sln`: başarılı, 0 hata, 0 uyarı. EXE smoke testi başarılı.

## 9 Ağustos 2026 Güncelleme ve Hakkında Ekranı

- ✅ Ana menüye `8 - Güncelleme / Hakkında` butonu eklendi.
- ✅ `AboutUpdateWindow` eklendi; hazırlayan bilgisi `Mehmet Akif SÖNMEZ`, firma bilgisi `İkizsoft Bilişim Hizmetleri` ve web adresi `www.ikizsoft.com` gösteriliyor.
- ✅ WPF uygulama sürümü proje dosyasından `0.9.0-dev` olarak okunur hale getirildi.
- ✅ Güncelleme kontrolü paneli eklendi; otomatik güncelleme sunucusu henüz bağlı olmadığı için kanal bilgisi `www.ikizsoft.com` olarak gösteriliyor.
- ✅ `dotnet build DersDagitim.sln`: başarılı, 0 hata, 0 uyarı.

## 9 Ağustos 2026 Manuel Düzenleme Görünürlüğü

- ✅ `DraftScheduleWindow` sağ detay paneline görünür `Manuel düzenleme` alanı eklendi.
- ✅ Seçili ders için `Yeni gün` ve `Yeni ders saati` seçilip `Seçili dersi bu saate taşı` butonuyla kalıcı taşıma yapılabiliyor.
- ✅ Seçili atamayı kaldırma butonu aynı manuel panel içinde tekrar gösterildi.
- ✅ Seçilen atamanın mevcut gün/saat bilgisi manuel düzenleme kontrollerine otomatik yansıyor.
- ✅ `dotnet build DersDagitim.sln`: başarılı, 0 hata, 0 uyarı.

## 9 Ağustos 2026 Kullanım Kılavuzu

- ✅ Sıfırdan kullanacak idareci için resimli kullanım kılavuzu hazırlandı.
- ✅ Çıktı dosyası: `docs/DersDagitim_Kullanim_Kilavuzu.docx`.
- ✅ Kılavuzda ana iş akışı, ASC XML içe/dışa aktarım, taslak üretme, manuel düzenleme, çakışma kontrolü, sınıf/kaynak çizelgeleri, öğretmen raporu, okul genel görünümü ve güncelleme/hakkında bölümü anlatıldı.
- ✅ Kılavuz yapısal kontrolü: 14 bölüm, 5 tablo, 6 görsel.
- ⚠️ DOCX render/PDF doğrulaması yapılamadı; bu ortamda `soffice` ve `winword` komutları bulunmuyor.

## 9 Ağustos 2026 Sınıf Alan Ataması

- ✅ Ana Excel kaynağı `C:\Users\masco\Desktop\bilisim_meslek_sinif_listesi2025 işletme.xlsx` okundu.
- ✅ Excel başlığından `BİLİŞİM TEKNOLOJİLERİ ALANI` bilgisi ve `12/C`, `12/D` sınıfları doğrulandı.
- ✅ ASC sınıf kodları ve ders adlarıyla alan eşlemesi tamamlandı: `(B/TB)=Bilişim Teknolojileri`, `(E/TE)=Elektrik-Elektronik Teknolojisi`, `(M/TBM)=Biyomedikal Cihaz Teknolojileri`.
- ✅ Canlı SQLite sınıf alanları transaction ile güncellendi: 46/46 sınıf atandı, boş alan kalmadı.
- ✅ Son dağılım: Bilişim Teknolojileri 20 sınıf, Elektrik-Elektronik Teknolojisi 14 sınıf, Biyomedikal Cihaz Teknolojileri 12 sınıf.
- ✅ Tekrarlanabilir araç eklendi: `tools/apply_class_departments_from_excel.py`.
- ✅ Yedekler: `backups/ders-dagitim-before-class-department-20260809-103743.db` ve `backups/ders-dagitim-before-class-department-20260809-103948.db`.
- ✅ `dotnet build DersDagitim.sln`: başarılı, 0 hata, 0 uyarı.
