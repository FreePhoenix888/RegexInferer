# RegexInferer

## Inference

...

## Simplification

Before:
```
href|target|rel|media|hreflang|type|sizes|content|name|src|charset|text|cite|ping|alt|sandbox|width|height|data|value|poster|coords|shape|scope|action|enctype|method|accept|max|min|pattern|placeholder|step|label|wrap|icon|radiogroup
```

After:
```
href(lang)?|(targe|tex|al|accep|heigh)t|rel|radiogroup|m(edia|ethod|ax|in)|s(izes|rc|andbox|tep|hape|cope)|c(ontent|harset|ite|oords)|name|p(ing|oster|attern|laceholder)|action|icon|width|wrap|data|value|(enc)?type|label
```

### Do not worth it:
```
oo
o{2}
```

```
action|icon
(acti|ic)on
```

```
target|type
t(arget|ype)
```

```
target|type|text
t(arget|ype|ext)
```

### Can be used:

```
media|method|max|min
m(edia|ethod|ax|in)
```

```
href|hreflang
href(lang)?
```

```
enctype|type
(enc)?type
```
