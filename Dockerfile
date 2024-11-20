FROM alpine:3.19
RUN echo 'http://dl-cdn.alpinelinux.org/alpine/edge/community/' >> /etc/apk/repositories
RUN apk update
RUN apk add --no-cache \
  bash                 \
  openssh              \
  git                  \
  just                 \
  rustup               \
  build-base           \
 	mingw-w64-gcc
RUN /usr/bin/rustup-init -y
RUN $HOME/.cargo/bin/cargo install bacon
RUN echo '. $HOME/.cargo/env' > $HOME/.bashrc
RUN $HOME/.cargo/bin/rustup target add x86_64-pc-windows-gnu
