project('yep-list', 'vala', 'c',
  version: '0.5.2',
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
  'src/Settings.vala',
  'src/TaskRow.vala',
  'src/TaskEditDialog.vala',
  'src/ListSidebar.vala',
  'src/CategoryManagerDialog.vala',
  'src/SettingsDialog.vala',
  'src/AboutDialog.vala',
  'src/RemoteLogger.vala',
)

executable('yep-list', sources,
  dependencies: dependencies,
  install: true,
)

# Install desktop file
install_data('data/com.github.kevinadams05.yeplist.desktop',
  install_dir: get_option('datadir') / 'applications',
)

# Install icon
install_data('data/yeplist.png',
  install_dir: get_option('datadir') / 'icons' / 'hicolor' / '256x256' / 'apps',
  rename: 'com.github.kevinadams05.yeplist.png',
)

# Install logo images for About dialog
install_data('data/logo-dark.png',
  install_dir: get_option('datadir') / 'yep-list',
)
install_data('data/logo-light.png',
  install_dir: get_option('datadir') / 'yep-list',
)

# Install changelog for About dialog
install_data('data/CHANGELOG.md',
  install_dir: get_option('datadir') / 'yep-list',
)
