"""Rebuild the lab 9 report, retaining its included title page and formatting.

Run from the project root:
  python3 report/make_report.py
Requires python-docx, Pillow and installed Arial / Times New Roman fonts on macOS.
The included lab9.docx is the formatting template; screenshots are genuine captures.
"""
from copy import deepcopy
from pathlib import Path
import re

import docx
from PIL import Image, ImageDraw, ImageFont
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_LINE_SPACING
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT / 'report' / 'build'
BUILD.mkdir(parents=True, exist_ok=True)
OUT = ROOT / 'lab9.docx'
FONT = '/System/Library/Fonts/Supplemental/Arial.ttf'


def diagram(name, width, height, draw):
    path = BUILD / name
    # Existing diagram assets remain stable when rebuilding only report text/screenshots.
    if path.exists():
        return path
    page = DiagramPage(width, height)
    draw(page)
    page.image.save(path)
    return path


class DiagramPage:
    def __init__(self, width, height):
        self.image = Image.new('RGB', (width * 2, height * 2), 'white')
        self.draw = ImageDraw.Draw(self.image)

    @staticmethod
    def color(rgb):
        return tuple(round(x * 255) for x in rgb) if rgb else None

    def draw_rect(self, rect, color, fill=None):
        self.draw.rectangle(tuple(round(x * 2) for x in rect),
                            outline=self.color(color), fill=self.color(fill), width=2)

    def draw_line(self, start, end, color, width=1):
        self.draw.line([tuple(x * 2 for x in p) for p in (start, end)],
                       fill=self.color(color), width=max(1, round(width * 2)))

    def draw_circle(self, center, radius, color, fill=None):
        x, y = center
        self.draw.ellipse(tuple(v * 2 for v in (x-radius, y-radius, x+radius, y+radius)),
                          outline=self.color(color), fill=self.color(fill), width=2)

    def draw_polyline(self, points, color, width=1):
        self.draw.line([tuple(x * 2 for x in p) for p in points],
                       fill=self.color(color), width=max(1, round(width * 2)))


def label(page, rect, text, size=12, align=0):
    font = ImageFont.truetype(FONT, round(size * 2))
    left, top, right, bottom = (v * 2 for v in rect)
    width = font.getlength(text)
    assert width <= right - left + 1, text
    x = left if align == 0 else (left + right - width) / 2 if align == 1 else right - width
    page.draw.text((x, top), text, font=font, fill=(20, 20, 20))


def scene_diagram(page):
    label(page, (10, 8, 610, 40), 'MainScene — объекты и компоненты', 22, 1)
    boxes = [
        ((12, 55, 603, 160), 'Main Camera', ['Camera (Orthographic)', 'Size = 5', 'AudioListener']),
        ((12, 178, 603, 310), 'Player', ['SpriteRenderer', 'Rigidbody2D; BoxCollider2D', 'PlayerController; CollisionReporter', 'Animator → Pulse']),
        ((12, 328, 603, 460), 'Enemy', ['SpriteRenderer', 'Rigidbody2D; CircleCollider2D', 'EnemyFollow', 'target → Player']),
    ]
    for rect, title, lines in boxes:
        page.draw_rect(rect, color=(0.25, 0.25, 0.25), fill=(0.97, 0.97, 0.97))
        x0, y0, x1, y1 = rect
        label(page, (x0 + 5, y0 + 10, x0 + 159, y0 + 45), title, 20, 1)
        page.draw_line((x0 + 164, y0), (x0 + 164, y1), color=(0.5, 0.5, 0.5))
        for i, line in enumerate(lines):
            label(page, (x0 + 177, y0 + 10 + i * 29, x1 - 5, y0 + 41 + i * 29), line, 20)
    label(page, (12, 477, 603, 507), 'Оба тела: Dynamic; Gravity Scale = 0', 20, 1)
    label(page, (12, 508, 603, 538), 'Is Trigger выключен на обоих коллайдерах', 20, 1)


def pulse_diagram(page):
    label(page, (0, 5, 610, 38), 'Pulse: масштаб X и Y за один цикл', 22, 1)
    left, right, top, bottom = 65, 575, 55, 195
    for value in (1.0, 1.25, 1.5):
        y = bottom - (value - 1) * (bottom - top) / 0.5
        page.draw_line((left, y), (right, y), color=(0.8, 0.8, 0.8), width=0.6)
        label(page, (4, y - 13, 58, y + 17), str(value).replace('.', ','), 20, 2)
    points = []
    for i in range(101):
        t = i / 100
        u = t * 2 if t <= 0.5 else (1 - t) * 2
        scale = 1 + 0.5 * (3 * u * u - 2 * u * u * u)
        points.append((left + t * (right - left), bottom - (scale - 1) * (bottom - top) / 0.5))
    page.draw_polyline(points, color=(0.1, 0.3, 0.55), width=2)
    page.draw_line((left, top - 10), (left, bottom + 6), color=(0.1, 0.1, 0.1))
    page.draw_line((left, bottom), (right + 10, bottom), color=(0.1, 0.1, 0.1))
    for t, value in [(0, 1), (0.5, 1.5), (1, 1)]:
        x = left + t * (right - left)
        y = bottom - (value - 1) * (bottom - top) / 0.5
        page.draw_circle((x, y), 3, color=(0.1, 0.3, 0.55), fill=(0.1, 0.3, 0.55))
        label(page, (x - 22, bottom + 10, x + 25, bottom + 42), str(t).replace('.', ','), 20, 1)
    label(page, (100, 250, 530, 283), 'Время, с; Loop Time = true', 20, 1)


scene_image = diagram('scene_structure.png', 615, 545, scene_diagram)
pulse_image = diagram('pulse_curve.png', 615, 290, pulse_diagram)

doc = docx.Document(ROOT / 'lab9.docx')
template_prefixes = {'body': 'Освоить базовые принципы',
                     'heading': '1 ЦЕЛЬ РАБОТЫ',
                     'subheading': '3.1 Подготовка проекта',
                     'conclusion': 'ВЫВОДЫ'}
templates = {name: deepcopy(next(p for p in doc.paragraphs
                                if p.text.startswith(prefix))._p.pPr)
             for name, prefix in template_prefixes.items()}
body = doc.element.body
cut = list(body).index(doc.paragraphs[39]._p)
for element in list(body)[cut + 1:]:
    if element.tag != qn('w:sectPr'):
        body.remove(element)

for index, text in [(20, 'Лабораторная работа № 9'),
                    (21, 'РАЗРАБОТКА ИНТЕРАКТИВНОГО ПРИЛОЖЕНИЯ В UNITY')]:
    p = doc.paragraphs[index]
    p.runs[0].text = text
    for run in p.runs[1:]:
        run.text = ''


def run(p, text, bold=False, italic=False, font='Times New Roman', size=14):
    r = p.add_run(text)
    r.font.name = font
    r.font.size = Pt(size)
    r.bold, r.italic = bold, italic
    r._element.get_or_add_rPr().rFonts.set(qn('w:cs'), font)
    return r


def rich(p, text, bold=False):
    for part in re.split(r'(\*[^*]+\*)', text):
        if part:
            run(p, part[1:-1] if part.startswith('*') else part,
                bold=bold, italic=part.startswith('*'))


def paragraph(text='', template='body', keep=False):
    p = doc.add_paragraph()
    p._p.insert(0, deepcopy(templates[template]))
    p.paragraph_format.keep_with_next = keep
    rich(p, text, bold=template != 'body')
    return p


def blank(keep=False):
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(0)
    p.paragraph_format.line_spacing_rule = WD_LINE_SPACING.EXACTLY
    p.paragraph_format.line_spacing = Pt(18)
    p.paragraph_format.keep_with_next = keep
    run(p, '')
    return p


def heading(text, conclusion=False, first=False):
    if not first:
        blank(keep=True)
    p = paragraph(text, 'conclusion' if conclusion else 'heading', keep=True)
    if first:
        p.paragraph_format.page_break_before = True
    blank(keep=True)


def subheading(text):
    blank(keep=True)
    paragraph(text, 'subheading', keep=True)
    blank(keep=True)


def code(text):
    lines = text.strip('\n').splitlines()
    blank(keep=True)
    for i, line in enumerate(lines):
        p = doc.add_paragraph()
        f = p.paragraph_format
        f.space_after = Pt(0)
        f.space_before = Pt(0)
        f.line_spacing = Pt(18)
        f.keep_together = True
        f.keep_with_next = i < len(lines) - 1 and (len(lines) < 16 or i < 2 or i >= len(lines) - 3)
        if '//' in line:
            left, right = line.split('//', 1)
            run(p, left)
            run(p, '//' + right, italic=True)
        else:
            run(p, line)
    blank()


figure_number = 0


def figure(path, caption, width_cm=16):
    global figure_number
    figure_number += 1
    blank(keep=True)
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.keep_with_next = True
    p.paragraph_format.space_after = Pt(0)
    p.paragraph_format.line_spacing = 1
    p.add_run().add_picture(str(path), width=Cm(width_cm))
    blank(keep=True)
    p = paragraph(f'Рисунок {figure_number} – {caption}')
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.first_line_indent = Cm(0)
    blank()


def screenshot(name, caption, before, after):
    """Include genuine screenshots only when the supplied image exists."""
    folder = ROOT / 'report' / 'screenshots'
    candidates = [folder / (name + ext) for ext in ('.png', '.jpg', '.jpeg')]
    image = next((p for p in candidates if p.is_file()), None)
    if image is None:
        return
    # Crop only surrounding UI; retain the original screenshot alongside the report.
    crops = {'scene': (0, 0, 3024, 1240),
             'collision': (740, 620, 1120, 880),
             'animation': (0, 1245, 2130, 1665)}
    if name in crops:
        with Image.open(image) as original:
            if original.size == (3024, 1898):
                cropped = BUILD / (name + '_report.png')
                original.crop(crops[name]).save(cropped)
                image = cropped
    paragraph(before.replace('{n}', str(figure_number + 1)))
    figure(image, caption, width_cm=9 if name == 'collision' else 16)
    paragraph(after)


heading('1 ЦЕЛЬ РАБОТЫ', first=True)
paragraph('Освоить базовые принципы разработки интерактивных приложений в среде *Unity*: '
          'создание 2D-сцены, программирование на языке *C#*, обработку пользовательского ввода, '
          'работу с анимацией и настройку физических взаимодействий объектов [1].')

heading('2 КРАТКИЕ ТЕОРЕТИЧЕСКИЕ СВЕДЕНИЯ')
paragraph('*Unity* — кроссплатформенный движок для создания 2D- и 3D-приложений. '
          'Сцена содержит объекты и описывает их начальное состояние. Окно *Hierarchy* '
          'отображает иерархию объектов, *Scene* используется для их размещения, '
          '*Inspector* — для настройки компонентов, *Project* — для работы с ресурсами, '
          'а *Game* показывает изображение игровой камеры.')
paragraph('Объект сцены представлен экземпляром *GameObject*. Его возможности определяются '
          'компонентами: *Transform* хранит положение, поворот и масштаб; *SpriteRenderer* '
          'отображает спрайт; *Rigidbody2D* задаёт физическое тело; *Collider2D* определяет '
          'форму столкновений. Пользовательский скрипт также является компонентом и '
          'наследуется от класса *MonoBehaviour*.')
paragraph('Метод *Awake* используется для начальной настройки компонента, *Start* вызывается '
          'один раз перед его первым обновлением, *Update* — каждый кадр. Физические '
          'изменения выполняются в *FixedUpdate*, который вызывается с фиксированным шагом '
          'времени. Для получения компонентов используется метод *GetComponent*.')
paragraph('Класс *Input* предоставляет доступ к клавиатуре. Метод *GetAxisRaw* возвращает '
          'значение настроенной оси без сглаживания, а *GetKey* проверяет удержание клавиши. '
          'Ось *Horizontal* соответствует движению по X, ось *Vertical* — по Y. '
          'Режим *Active Input Handling = Both* разрешает старую систему *Input Manager* '
          'и новую *Input System*. В работе используется старый интерфейс *UnityEngine.Input*.')
paragraph('Скорость измеряется в единицах сцены в секунду. Для вычисления перемещения '
          'скорость умножают на прошедшее время: *Time.deltaTime* в кадровом цикле или '
          '*Time.fixedDeltaTime* в физическом. При присваивании *Rigidbody2D.linearVelocity* '
          'задаётся сама скорость, поэтому дополнительно умножать её на время не нужно. '
          'Нормализация вектора ввода исключает ускорение при движении по диагонали.')
paragraph('Анимационный клип хранит ключевые значения свойств и интерполяцию между ними. '
          'Компонент *Animator* воспроизводит клипы с помощью контроллера состояний. '
          'Для пульсации достаточно анимировать масштаб объекта и включить повторение '
          'клипа. Обработчики *OnCollisionEnter2D* и *OnCollisionExit2D* позволяют '
          'реагировать на начало и окончание физического контакта.')

heading('3 ВЫПОЛНЕНИЕ РАБОТЫ')
paragraph('Разработано интерактивное 2D-приложение с управляемым квадратом *Player* '
          'и автоматически движущимся к нему кругом *Enemy*. Выполнены базовая часть '
          'и три дополнительных задания: ускорение, преследование и физические столкновения. '
          'Дополнительно реализовано изменение цвета игрока при контакте с противником. '
          'Оформление отчёта выполнено с учётом требований СТП 01–2024 [2].')

subheading('3.1 Подготовка проекта и создание сцены')
paragraph('Используется редактор *Unity 6000.6.8f1*, ревизия *237b6593792b*, установленный '
          'на *macOS*. Эта версия совпадает с записью в *ProjectSettings/ProjectVersion.txt*. '
          'Основная сцена сохранена в *Assets/Scenes/MainScene.unity* и добавлена в '
          'список сцен сборки. В настройках проекта включён режим ввода *Both*.')
paragraph('Камера настроена на ортографическую проекцию с размером 5 и расположена '
          'в точке (0; 0; −10). Игрок имеет начальную позицию (−2; 0; 0), '
          'противник — (2; 1,5; 0). Для объектов назначены спрайты квадрата и круга. '
          'Основные объекты сцены доступны в окне *Hierarchy*.')
screenshot('scene', 'Сцена MainScene в редакторе Unity',
           'Общий вид подготовленной сцены в редакторе представлен на рисунке {n}. '
           'В иерархии показаны объекты, используемые в приложении.',
           'Игрок и противник размещены в области обзора игровой камеры. '
           'Компоненты выбранного объекта доступны для настройки в окне *Inspector*.')
paragraph('Скрипты находятся в папке *Assets/Scripts*, а анимационный клип и контроллер — '
          'в *Assets/Animations*. Скрипт *EnemyFollow* содержит ссылку *target* '
          'на компонент *Transform* игрока. Противнику назначен тег *Enemy*, '
          'который используется при обработке столкновений.')

subheading('3.2 Управление движением и вращением игрока')
paragraph('Скрипт *PlayerController* получает *Rigidbody2D* в методе *Awake*, '
          'отключает гравитацию и свободное вращение от физических воздействий. '
          'Для более плавного отображения включена интерполяция, '
          'для обнаружения быстрых столкновений — режим *Continuous*.')
paragraph('В методе *Update* считываются оси управления, клавиши ускорения и поворота. '
          'Вектор движения нормализуется. Клавиша Q задаёт положительное направление '
          'вращения, E — отрицательное; при одновременном нажатии они компенсируют друг друга. '
          'Соответствующий фрагмент скрипта:')
code('''movement = new Vector2(
    Input.GetAxisRaw("Horizontal"),
    Input.GetAxisRaw("Vertical")).normalized;

isSprinting = Input.GetKey(KeyCode.LeftShift)
    || Input.GetKey(KeyCode.RightShift);
rotationInput = 0f;
if (Input.GetKey(KeyCode.Q)) rotationInput += 1f;
if (Input.GetKey(KeyCode.E)) rotationInput -= 1f;''')
paragraph('Движение направлено по мировым осям и не зависит от поворота квадрата. '
          'Линейная скорость и управляемый поворот задаются в *FixedUpdate*:')
code('''private void FixedUpdate()
{
    float currentSpeed = moveSpeed;
    if (isSprinting)
        currentSpeed *= sprintMultiplier;

    rb.linearVelocity = movement * currentSpeed;
    rb.MoveRotation(rb.rotation
        + rotationInput * rotateSpeed * Time.fixedDeltaTime);
}''')
paragraph('Параметры движения доступны в *Inspector*: *moveSpeed = 5*, '
          '*sprintMultiplier = 2*, *rotateSpeed = 180*. В используемой версии '
          '*Unity* применяется свойство *linearVelocity*, соответствующее '
          '*velocity* в примере из методических указаний.')

subheading('3.3 Задание 1. Ускорение при удержании Shift')
paragraph('При удержании левой или правой клавиши *Shift* переменная *isSprinting* '
          'принимает значение *true*. В физическом цикле базовая скорость умножается '
          'на *sprintMultiplier*. Таким образом, обычная скорость составляет '
          '5 единиц/с, а скорость ускоренного движения — 10 единиц/с. '
          'После отпускания клавиши снова используется базовая скорость; '
          'изменение частоты кадров не меняет заданные значения скорости.')

subheading('3.4 Задание 2. Автоматическое преследование игрока')
paragraph('Для противника создан скрипт *EnemyFollow* с параметрами *target* '
          'и *followSpeed = 2*. Метод *Vector2.MoveTowards* вычисляет точку '
          'направленного перемещения к игроку на расстояние не более '
          '*followSpeed × Time.fixedDeltaTime*. Скорость динамического тела '
          'получается делением требуемого смещения на длительность шага:')
code('''private void FixedUpdate()
{
    if (target == null)
    {
        rb.linearVelocity = Vector2.zero;
        return;
    }

    Vector2 nextPosition = Vector2.MoveTowards(
        rb.position, target.position,
        followSpeed * Time.fixedDeltaTime);
    rb.linearVelocity = (nextPosition - rb.position)
        / Time.fixedDeltaTime;
}''')
paragraph('Проверка *target == null* предотвращает обращение к отсутствующему объекту '
          'и останавливает противника. При свободном движении задана постоянная '
          'скорость 2 единицы/с. В непосредственной близости шаг ограничивается '
          'расстоянием до цели, а фактическое движение при контакте определяется '
          'также решателем физических столкновений.')

subheading('3.5 Задание 3. Физика и обработка столкновений')
paragraph('Оба объекта имеют динамический *Rigidbody2D* с нулевым масштабом гравитации. '
          'Форма игрока задаётся *BoxCollider2D* размером 1 × 1, форма противника — '
          '*CircleCollider2D* радиусом 0,5. Флаг *Is Trigger* выключен, поэтому '
          'коллайдеры участвуют в обычных физических контактах.')
paragraph('В отличие от перемещения через *Transform.Translate*, установка скорости '
          'тела передаёт управление перемещением физическому движку. Поворот игрока '
          'также выполняется через *Rigidbody2D.MoveRotation*. Масштаб коллайдера '
          'игрока изменяется вместе с пульсацией объекта.')
paragraph('Компонент *CollisionReporter* сохраняет исходный цвет спрайта в *Awake*. '
          'Если объект с тегом *Enemy* входит в контакт с игроком, цвет изменяется '
          'на красный. После завершения контакта восстанавливается исходное значение:')
code('''private void OnCollisionEnter2D(Collision2D collision)
{
    if (collision.gameObject.CompareTag("Enemy")
        && spriteRenderer != null)
        spriteRenderer.color = Color.red;
}

private void OnCollisionExit2D(Collision2D collision)
{
    if (collision.gameObject.CompareTag("Enemy")
        && spriteRenderer != null)
        spriteRenderer.color = defaultColor;
}''')
screenshot('collision', 'Контакт игрока и противника в режиме Play',
           'Визуальная реакция на контакт объектов показана на рисунке {n}.',
           'Красный цвет игрока соответствует обработке события *OnCollisionEnter2D*. '
           'При завершении контакта *OnCollisionExit2D* восстанавливает исходный цвет.')

subheading('3.6 Создание анимации пульсации')
paragraph('На игроке установлен *Animator* с контроллером *PlayerAnimator*. '
          'Начальным состоянием контроллера является *Pulse*, которое воспроизводит '
          'одноимённый анимационный клип. Пульсация задаётся ключами '
          '*Transform.localScale*: на отметке 0 с масштаб равен (1; 1; 1), '
          'на 0,5 с — (1,5; 1,5; 1), на 1 с — снова (1; 1; 1). '
          f'Ключи и изменение масштаба показаны на рисунке {figure_number + 1}.')
figure(pulse_image, 'Кривая масштаба по данным клипа Pulse')
paragraph('Для клипа включён параметр *Loop Time*, поэтому цикл повторяется '
          'автоматически. Между ключами выполняется плавная интерполяция. '
          'Масштаб по Z остаётся равным 1. Анимация не содержит ключей позиции '
          'и поворота, поэтому не переопределяет управление движением. '
          'Её можно просматривать и редактировать в окне *Animation* без '
          'написания отдельного скрипта пульсации.')
screenshot('animation', 'Анимационный клип Pulse в окне Animation',
           'Настройка ключевых кадров клипа в редакторе приведена на рисунке {n}.',
           'Дорожки масштаба связывают анимацию с компонентом *Transform* игрока. '
           'Клип воспроизводится через состояние *Pulse* контроллера *PlayerAnimator*.')

subheading('3.7 Проверка проекта и порядок запуска')
paragraph('Проверены совпадение версии редактора и проекта, корректность ссылок '
          'сцены на скрипты, спрайты и контроллер анимации, наличие коллайдеров, '
          'параметры камеры и повторение клипа. Все три скрипта скомпилированы '
          'компилятором *C#* с библиотеками установленной *Unity 6000.6.8f1* '
          'без ошибок компиляции.')
paragraph('Компиляция и проверка ресурсов подтверждают корректность кода и структуры '
          'проекта. Сцена запущена в *Unity Editor*: наблюдались автоматическая '
          'пульсация игрока, приближение противника и изменение цвета при '
          'контакте объектов. Результат столкновения зафиксирован на скриншоте. '
          'При остановке *Play* восстановились исходные позиции и масштаб. '
          'Ниже приведены дополнительные сценарии проверки управления '
          'и ожидаемые результаты.')
for item_number, text in enumerate([
    'Открыть проект через *Unity Hub*, затем двойным щелчком открыть '
    '*Assets/Scenes/MainScene.unity*. В *Hierarchy* должны присутствовать '
    '*Main Camera*, *Player* и *Enemy*.',
    'Нажать *Play* и активировать окно *Game*. Квадрат должен пульсировать, '
    'а круг — двигаться в его сторону.',
    'Проверить движение клавишами *WASD* и стрелками. При диагональном '
    'движении модуль заданной скорости должен оставаться таким же, как при движении по одной оси.',
    'Удержать *Shift*: скорость должна увеличиться вдвое. Проверить повороты '
    'клавишами *Q* и *E*.',
    'Дождаться контакта объектов: они должны физически взаимодействовать, '
    'а игрок — изменить цвет. После расхождения должен восстановиться исходный цвет.',
    'Остановить *Play*: объекты должны вернуться к состоянию сохранённой сцены. '
    'Границы уровня не предусмотрены; при выходе игрока за пределы камеры '
    'повторный запуск возвращает начальные позиции.',
], start=1):
    paragraph(str(item_number) + ' ' + text)

subheading('3.8 Ответы на контрольные вопросы')
for question, answer in [
    ('1. В чём отличие методов Start и Update?',
     '*Start* вызывается один раз перед первым обновлением активного компонента. '
     '*Update* вызывается каждый кадр, пока компонент включён и объект активен.'),
    ('2. Для чего используется Time.deltaTime?',
     'Это длительность прошедшего кадра. Умножение скорости на время даёт '
     'перемещение за кадр и позволяет избежать зависимости скорости от *FPS*. '
     'В физическом цикле используется *Time.fixedDeltaTime*.'),
    ('3. Какие клавиши обрабатывает Input.GetAxis("Horizontal")?',
     'В стандартных настройках *Input Manager* — A и D, стрелки влево и вправо. '
     'Отрицательное значение соответствует движению влево, положительное — вправо.'),
    ('4. Что такое компонент и как он связан с GameObject?',
     'Компонент хранит данные или определяет поведение объекта. '
     'Один *GameObject* объединяет несколько компонентов; например, '
     '*SpriteRenderer* отображает спрайт, а *Rigidbody2D* добавляет физическое тело.'),
    ('5. Как создать анимацию без написания кода?',
     'Выбрать объект, открыть окно *Animation*, создать клип и включить запись. '
     'На временной шкале задать ключевые значения свойства, затем остановить '
     'запись. Для повторения включить *Loop Time* и назначить клип состоянию '
     'контроллера *Animator*.'),
    ('6. Зачем нужна настройка Active Input Handling = Both?',
     'Она разрешает работу старого *Input Manager* и новой *Input System* '
     'одновременно. В данной работе это обеспечивает поддержку вызовов '
     '*UnityEngine.Input* из скриптов методички.'),
]:
    p = paragraph()
    rich(p, question, bold=True)
    p.paragraph_format.keep_with_next = True
    paragraph(answer)

heading('ВЫВОДЫ', conclusion=True)
paragraph('В ходе лабораторной работы освоены основные элементы редактора *Unity*, '
          'компонентный подход к построению сцены и программирование поведения '
          'объектов на языке *C#*. Подготовлена 2D-сцена с управляемым игроком '
          'и преследующим его противником.')
paragraph('Реализованы движение и вращение игрока, удвоение скорости при удержании '
          '*Shift*, автоматическое преследование через *Vector2.MoveTowards*, '
          'зацикленная анимация масштаба и физические столкновения. '
          'Дополнительно предусмотрено изменение цвета игрока при контакте '
          'с противником. Ввод отделён от физического обновления, '
          'а движение выполняется через *Rigidbody2D*.')
paragraph('Версия проекта согласована с установленным редактором '
          '*Unity 6000.6.8f1*. Компиляция скриптов и статическая проверка '
          'ресурсов выполнены успешно. В режиме *Play* подтверждены '
          'пульсация, преследование и визуальная реакция на столкновение. '
          'Для проверки клавиатурного управления приведены отдельные сценарии.')

heading('СПИСОК ИСПОЛЬЗОВАННЫХ ИСТОЧНИКОВ', conclusion=True)
paragraph('1 Разработка интерактивного приложения в Unity [Электронный ресурс] : '
          'методические указания к лабораторной работе. – Локальный файл: Lab10_Unity.docx.')
paragraph('2 Дипломные проекты (работы). Общие требования : СТП 01–2024. – '
          'Минск : БГУИР, 2024. – 178 с. – Электронная копия: '
          'https://ru.scribd.com/document/910898315. – Дата доступа: 09.11.2026.')


def apply_stp_formatting():
    """STP 01–2024, sections 2.1, 2.2, 2.3, 2.5 and Appendix Л.

    User exceptions: no contents and no forced section breaks.
    Keep the title-page layout from lab8; normalize the report body explicitly.
    """
    for section in doc.sections:
        section.page_width, section.page_height = Cm(21), Cm(29.7)
        section.left_margin, section.right_margin = Cm(3), Cm(1.5)
        section.top_margin = section.bottom_margin = Cm(2)
        section.footer_distance = Cm(1)
        section.different_first_page_header_footer = True
        # PAGE counts the title page, whose first-page footer is empty.
        for element in list(section.footer._element):
            section.footer._element.remove(element)
        p = section.footer.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        p.paragraph_format.space_before = p.paragraph_format.space_after = Pt(0)
        p.paragraph_format.line_spacing = Pt(18)
        field = OxmlElement('w:fldSimple')
        field.set(qn('w:instr'), 'PAGE')
        field_run = OxmlElement('w:r')
        properties = OxmlElement('w:rPr')
        fonts = OxmlElement('w:rFonts')
        for key in ('ascii', 'hAnsi', 'cs', 'eastAsia'):
            fonts.set(qn('w:' + key), 'Times New Roman')
        properties.append(fonts)
        size = OxmlElement('w:sz')
        size.set(qn('w:val'), '28')
        properties.append(size)
        field_run.append(properties)
        number = OxmlElement('w:t')
        number.text = '2'
        field_run.append(number)
        field.append(field_run)
        p._p.append(field)
        for element in list(section.first_page_footer._element):
            section.first_page_footer._element.remove(element)
        section.first_page_footer.add_paragraph()

    normal = doc.styles['Normal']
    normal.font.name = 'Times New Roman'
    normal.font.size = Pt(14)
    normal.paragraph_format.space_before = normal.paragraph_format.space_after = Pt(0)
    normal.paragraph_format.line_spacing = Pt(18)

    bold_font = ImageFont.truetype('/System/Library/Fonts/Supplemental/Times New Roman Bold.ttf', 140)
    for p in doc.paragraphs[40:]:
        f = p.paragraph_format
        f.space_before = f.space_after = Pt(0)
        f.widow_control = True
        f.left_indent = f.right_indent = Cm(0)
        has_image = bool(p._p.xpath('.//w:drawing'))
        f.line_spacing = 1 if has_image else Pt(18)
        is_heading = bool(re.match(r'^\d+(?:\.\d+)? ', p.text)) and bool(p.runs and p.runs[0].bold)
        is_center_heading = p.text in ('ВЫВОДЫ', 'СПИСОК ИСПОЛЬЗОВАННЫХ ИСТОЧНИКОВ')
        is_caption = p.text.startswith('Рисунок ')
        is_body = p._p.pPr is not None and p._p.pPr.find(qn('w:jc')) is not None and f.first_line_indent is not None and f.first_line_indent > 0
        if is_heading:
            prefix = re.match(r'^\d+(?:\.\d+)? ', p.text).group()
            prefix_width = bold_font.getlength(prefix) / 10
            # Number starts at the paragraph indent; wrapped text aligns with its first letter.
            f.left_indent = Cm(1.25) + Pt(prefix_width)
            f.first_line_indent = -Pt(prefix_width)
            p.alignment = WD_ALIGN_PARAGRAPH.LEFT
            f.keep_with_next = True
            f.keep_together = True
        elif is_center_heading or is_caption or has_image:
            f.first_line_indent = Cm(0)
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            f.keep_together = True
            if is_center_heading:
                f.keep_with_next = True
        elif is_body:
            f.first_line_indent = Cm(1.25)
            p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
            if p.text.endswith(':') and len(p.text) < 250:
                f.keep_with_next = True
        else:
            f.first_line_indent = Cm(0)
            p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        if is_heading or is_center_heading or is_caption:
            no_hyphen = p._p.get_or_add_pPr().find(qn('w:suppressAutoHyphens'))
            if no_hyphen is None:
                no_hyphen = OxmlElement('w:suppressAutoHyphens')
                p._p.get_or_add_pPr().append(no_hyphen)
        for r in p.runs:
            r.font.name = 'Times New Roman'
            r.font.size = Pt(14)
        # User explicitly requested continuous sections; title remains separate.
        f.page_break_before = p.text == '1 ЦЕЛЬ РАБОТЫ'


apply_stp_formatting()

# Preserve the template's title page, footer and section settings; remove old pictures.
xml = doc.element.body.xml
for rid, rel in list(doc.part.rels.items()):
    if rel.reltype.endswith('/image') and f'"{rid}"' not in xml:
        doc.part.drop_rel(rid)
doc.core_properties.title = 'Лабораторная работа № 9. Разработка интерактивного приложения в Unity'
doc.core_properties.subject = 'Разработка 2D-приложения в Unity'
doc.core_properties.author = 'Арсенович П. А.; Малайчук В. В.'
doc.core_properties.keywords = 'Unity, C#, лабораторная работа 9'
doc.core_properties.comments = ''
doc.save(OUT)
print(OUT)
