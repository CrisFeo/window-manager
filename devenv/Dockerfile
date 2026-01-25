FROM alpine:3.19

ARG USER=root
ARG USER_UID=
ARG USER_GID=
ARG DOCKER_GID=

RUN [ ! -z $USER_UID ] && addgroup -g $USER_GID $USER || :
RUN [ ! -z $USER_UID ] && adduser -u $USER_UID -G $USER -D -h /home/$USER $USER || :
RUN [ ! -z $DOCKER_GID ] && addgroup -g $DOCKER_GID docker || :
RUN [ ! -z $DOCKER_GID ] && addgroup $USER docker || :
RUN [ ! -z $USER ] && echo "$USER ALL=(ALL) NOPASSWD: ALL" >> /etc/sudoers || :

RUN echo 'http://dl-cdn.alpinelinux.org/alpine/edge/community/' >> /etc/apk/repositories
RUN apk update
RUN apk add --no-cache \
  coreutils            \
  bash                 \
  less                 \
  tmux                 \
  ripgrep              \
  kakoune              \
  openssh              \
  git                  \
  build-base           \
  rustup               \
  mingw-w64-gcc

USER $USER

RUN /usr/bin/rustup-init -y
RUN $HOME/.cargo/bin/rustup target add x86_64-pc-windows-gnu
RUN echo 'export PS1="\w\n\[\e[;33m\]>\[\e[m\] "' >> $HOME/.profile
RUN echo 'export EDITOR=kak' >> $HOME/.profile
