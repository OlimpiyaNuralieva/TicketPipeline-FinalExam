#ifndef TICKETCOST_H
#define TICKETCOST_H

#include <stdint.h>

#if defined(_WIN32)
#  define TICKETCOST_API __declspec(dllexport)
#else
#  define TICKETCOST_API __attribute__((visibility("default")))
#endif

#define TICKETCOST_ERR_KIND   (-1)
#define TICKETCOST_ERR_EFFORT (-2)

TICKETCOST_API int32_t ticket_cost_cents(int32_t kind, int32_t effort);

#endif
