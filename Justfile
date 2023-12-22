project_name := 'window-manager'
cargo_args := '--target=x86_64-pc-windows-gnu'

list:
  just -l -u

setup:
  cargo install --locked just
  cargo install --locked bacon

build:
  cargo build {{cargo_args}}

deploy:
  just build
  cp ./target/x86_64-pc-windows-gnu/debug/{{project_name}}.exe /c/temp/

check:
  cargo clippy {{cargo_args}}

watch task='check':
  bacon clippy -- {{cargo_args}}
