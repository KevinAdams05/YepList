project('yep-list', 'vala', 'c',
  version: '1.0.0',
  meson_version: '>= 0.62.0',
)

gnome = import('gnome')

dependencies = [
  dependency('glib-2.0'),
  dependency('gobject-2.0'),
  dependency('gio-2.0'),
  dependency('gtk4'),
  dependency('libadwaita-1', version: '>= 1.4'),
  dependency('libsoup-3.0'),
  dependency('json-glib-1.0'),
]

sources = files(
  'src/Application.vala',
  'src/MainWindow.vala',
  'src/Models.vala',
  'src/ApiClient.vala',
  'src/TaskRow.vala',
  'src/TaskEditDialog.vala',
  'src/ListSidebar.vala',
  'src/CategoryManagerDialog.vala',
)

executable('yep-list', sources,
  dependencies: dependencies,
  install: true,
)

# Install icon
install_data('data/yeplist.png',
  install_dir: get_option('datadir') / 'icons' / 'hicolor' / '256x256' / 'apps',
  rename: 'com.github.kevinadams05.yeplist.png',
)
